using System;
using System.Text;
using Android.Content;
using Android.Security.Keystore;
using AndroidX.Preference;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

namespace AuthenticatorPro.Data
{
    internal class DatabasePasswordStorage
    {
        private const string KeyStoreName = "AndroidKeyStore";
        private const string KeyAlias = "databasePassphrase";
        
        private const string PasswordPrefKey = "databasePassphrase";
        private const string IvPrefKey = "databasePassphraseIv";
        
        // TODO: check api < 23
        private const string Algorithm = KeyProperties.KeyAlgorithmAes;
        private const string BlockMode = KeyProperties.BlockModeCbc;
        private const string Padding = KeyProperties.EncryptionPaddingPkcs7;
        private const string Transformation = Algorithm + "/" + BlockMode + "/" + Padding;

        private readonly ISharedPreferences _preferences;
        
        public DatabasePasswordStorage(Context context)
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
                
            var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, "AndroidKeyStore");
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
            GenerateKey();
            var cipher = Cipher.GetInstance(Transformation);
            cipher.Init(CipherMode.EncryptMode, GetKeyFromKeyStore());
            return cipher;
        }
        
        public Cipher GetDecryptionCipher()
        {
            var cipher = Cipher.GetInstance(Transformation);
            var iv = GetNullableByteArrayPreference(IvPrefKey, null); 
            cipher.Init(CipherMode.DecryptMode, GetKeyFromKeyStore(), new IvParameterSpec(iv));
            return cipher;
        }

        public void Store(string password, Cipher cipher)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var iv = cipher.GetIV();
            var payload = cipher.DoFinal(passwordBytes);
            
            SetNullableByteArrayPreference(IvPrefKey, iv); 
            SetNullableByteArrayPreference(PasswordPrefKey, payload); 
        }

        public static void Clear()
        {
            var ks = KeyStore.GetInstance(KeyStoreName);
            ks.Load(null);
            ks.DeleteEntry(KeyAlias);
        }

        public string Fetch(Cipher cipher)
        {
            var payload = GetNullableByteArrayPreference(PasswordPrefKey, null);
            var result = cipher.DoFinal(payload);
            return result == null ? null : Encoding.UTF8.GetString(result);
        }
        
        private byte[]? GetNullableByteArrayPreference(string key, byte[]? defaultValue)
        {
            var value = _preferences.GetString(key, null);

            return value == null
                ? defaultValue
                : Convert.FromBase64String(value);
        }
        
        private void SetNullableByteArrayPreference(string key, byte[]? value)
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