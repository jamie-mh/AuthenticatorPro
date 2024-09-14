// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Serilog;

namespace Stratum.Droid.Storage
{
    public class BiometricStorage
    {
        private const string KeyStoreName = "AndroidKeyStore";
        private const string KeyAlias = "databasePassphrase";

        private const string PasswordPrefKey = "databasePassphrase";
        private const string IvPrefKey = "databasePassphraseIv";

        private readonly ILogger _log = Log.ForContext<BiometricStorage>();
        private readonly ISharedPreferences _preferences;
        private readonly object _lock = new();

        public BiometricStorage(Context context)
        {
            var preferenceAlias = $"{context.PackageName}_biometrics";
            _preferences = context.GetSharedPreferences(preferenceAlias, FileCreationMode.Private);
        }

        private static void GenerateKey()
        {
#pragma warning disable CA1416
            var specBuilder =
                new KeyGenParameterSpec.Builder(KeyAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                    .SetBlockModes(BlockMode)
                    .SetEncryptionPaddings(Padding)
                    .SetUserAuthenticationRequired(true);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                specBuilder = specBuilder.SetInvalidatedByBiometricEnrollment(true);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                specBuilder =
                    specBuilder.SetUserAuthenticationParameters(0, (int) KeyPropertiesAuthType.BiometricStrong);
            }
            else
            {
#pragma warning disable CA1422
                specBuilder = specBuilder.SetUserAuthenticationValidityDurationSeconds(-1);
#pragma warning restore CA1422
            }

            var spec = specBuilder.Build();
#pragma warning restore CA1416

            var generator = KeyGenerator.GetInstance(Algorithm, KeyStoreName);
            generator.Init(spec);
            generator.GenerateKey();
        }

        private IKey GetKeyFromKeyStore()
        {
            lock (_lock)
            {
                var ks = KeyStore.GetInstance(KeyStoreName);
                ks.Load(null);
                return ks.GetKey(KeyAlias, null);
            }
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
            catch (KeyPermanentlyInvalidatedException)
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

            if (iv == null || payload == null)
            {
                Clear();
                throw new InvalidOperationException("Encryption failed, no result");
            }

            SetByteArrayPreference(IvPrefKey, iv);
            SetByteArrayPreference(PasswordPrefKey, payload);
        }

        public void Clear()
        {
            lock (_lock)
            {
                var ks = KeyStore.GetInstance(KeyStoreName);
                ks.Load(null);

                try
                {
                    ks.DeleteEntry(KeyAlias);
                }
                catch (KeyStoreException e)
                {
                    // Perhaps the key doesn't exist?
                    _log.Error(e, "Failed to clear keystore entry");
                }
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

#pragma warning disable CA1416
        private const string Algorithm = KeyProperties.KeyAlgorithmAes;
        private const string BlockMode = KeyProperties.BlockModeCbc;
        private const string Padding = KeyProperties.EncryptionPaddingPkcs7;
        private const string Transformation = Algorithm + "/" + BlockMode + "/" + Padding;
#pragma warning restore CA1416
    }
}