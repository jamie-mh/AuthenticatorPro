using System;
using System.Text;
using Android.Content;
using Android.Security.Keystore;
using AndroidX.Preference;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

namespace AuthenticatorPro.Droid.Util
{
    internal class PasswordStorageManager
    {
        private const string KeyStoreName = "AndroidKeyStore";
        private const string KeyAlias = "databasePassphrase";
        
        private const string PasswordPrefKey = "databasePassphrase";
        private const string IvPrefKey = "databasePassphraseIv";
        
        private const string Algorithm = KeyProperties.KeyAlgorithmAes;
        private const string BlockMode = KeyProperties.BlockModeCbc;
        private const string Padding = KeyProperties.EncryptionPaddingPkcs7;
        private const string Transformation = Algorithm + "/" + BlockMode + "/" + Padding;

        private readonly ISharedPreferences _preferences;
        
        public PasswordStorageManager(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }

        private static void GenerateKey()
        {
            var spec = new KeyGenParameterSpec.Builder(KeyAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetBlockModes(BlockMode)
                .SetEncryptionPaddings(Padding)
                .SetUserAuthenticationRequired(true)
                .Build();
                
            var generator = KeyGenerator.GetInstance(Algorithm, KeyStoreName);
            generator.Init(spec);
            generator.GenerateKey();
        }
        
        private static IKey GetKeyFromKeyStore()
        {
            var ks = KeyStore.GetInstance(KeyStoreName);
            ks.Load(null);
            return ks.GetKey(KeyAlias, null);
        }

        public Cipher GetEncryptionCipher()
        {
            try
            {
                GenerateKey();
            }
            catch
            {
                Clear();
                throw;
            }
            
            var cipher = Cipher.GetInstance(Transformation);

            try
            {
                cipher.Init(CipherMode.EncryptMode, GetKeyFromKeyStore());
            }
            catch(KeyPermanentlyInvalidatedException)
            {
                Clear();
                throw;
            }
            
            return cipher;
        }
        
        public Cipher GetDecryptionCipher()
        {
            var cipher = Cipher.GetInstance(Transformation);
            var iv = GetByteArrayPreference(IvPrefKey, null); 
            cipher.Init(CipherMode.DecryptMode, GetKeyFromKeyStore(), new IvParameterSpec(iv));
            return cipher;
        }

        public void Store(string password, Cipher cipher)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var iv = cipher.GetIV();
            var payload = cipher.DoFinal(passwordBytes);

            if(iv == null || payload == null)
            {
                Clear();
                throw new Exception("Encryption failed, no result");
            }
            
            SetByteArrayPreference(IvPrefKey, iv); 
            SetByteArrayPreference(PasswordPrefKey, payload); 
        }

        public void Clear()
        {
            var ks = KeyStore.GetInstance(KeyStoreName);
            ks.Load(null);

            try
            {
                ks.DeleteEntry(KeyAlias);
            }
            catch(KeyStoreException)
            {
                // Perhaps the key doesn't exist? 
            }
            
            SetByteArrayPreference(PasswordPrefKey, null);
            SetByteArrayPreference(IvPrefKey, null);
        }

        public string Fetch(Cipher cipher)
        {
            var payload = GetByteArrayPreference(PasswordPrefKey, null);
            var result = cipher.DoFinal(payload);
            return result == null ? null : Encoding.UTF8.GetString(result);
        }
        
        private byte[] GetByteArrayPreference(string key, byte[] defaultValue)
        {
            var value = _preferences.GetString(key, null);

            return value == null
                ? defaultValue
                : Convert.FromBase64String(value);
        }
        
        private void SetByteArrayPreference(string key, byte[] value)
        {
            var valueStr = value switch
            {
                null => null,
                _ => Convert.ToBase64String(value)
            };

            _preferences.Edit().PutString(key, valueStr).Commit();
        }
    }
}