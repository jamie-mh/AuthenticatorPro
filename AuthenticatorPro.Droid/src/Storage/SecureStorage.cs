// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

// Adapted from Xamarin.Essentials (MIT License) for backwards compatibility
// https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/SecureStorage/SecureStorage.android.cs

using System;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Security;
using Android.Security.Keystore;
using Java.Math;
using Java.Security;
using Java.Util;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Javax.Security.Auth.X500;
using Serilog;

namespace AuthenticatorPro.Droid.Storage
{
    public class SecureStorage
    {
        private const string KeyStoreName = "AndroidKeyStore";
        private const string AsymmetricCipher = "RSA/ECB/PKCS1Padding";
        private const string SymmetricCipher = "AES/GCM/NoPadding";
        private const string AsymmetricAlgorithm = "RSA";
        private const string SymmetricAlgorithm = "AES";
        private const int IvLength = 12; // Android supports an IV of 12 for AES/GCM

        private const string MasterKeyPreferenceKey = "SecureStorageKey";
        private const string UseSymmetricPreferenceKey = "essentials_use_symmetric";

        private readonly ILogger _log = Log.ForContext<SecureStorage>();
        private readonly Context _context;
        private readonly string _preferenceAlias;
        private readonly ISharedPreferences _preferences;
        private readonly object _lock = new();

        public SecureStorage(Context context)
        {
            _context = context;
            _preferenceAlias = $"{context.PackageName}.xamarinessentials";
            _preferences = context.GetSharedPreferences(_preferenceAlias, FileCreationMode.Private);
        }

        public string Get(string key)
        {
            var encryptedString = _preferences.GetString(key, null);

            if (encryptedString == null)
            {
                return null;
            }

            var encryptedBytes = Convert.FromBase64String(encryptedString);

            try
            {
                return Decrypt(encryptedBytes);
            }
            catch (AEADBadTagException e)
            {
                _log.Error(e, "Unable to decrypt value for key {Key}", key);
                _preferences.Edit().Remove(key).Commit();
                return null;
            }
        }

        public void Set(string key, string data)
        {
            if (data == null)
            {
                _preferences.Edit().Remove(key).Commit();
                return;
            }

            var encryptedData = Encrypt(data);
            var encryptedString = Convert.ToBase64String(encryptedData);

            _preferences.Edit().PutString(key, encryptedString).Commit();
        }

        private static KeyStore GetKeyStore()
        {
            var keyStore = KeyStore.GetInstance(KeyStoreName);
            keyStore.Load(null);

            return keyStore;
        }

        private ISecretKey GetKey()
        {
            // check to see if we need to get our key from past-versions or newer versions.
            // we want to use symmetric if we are >= 23 or we didn't set it previously.

            var useSymmetric = _preferences.GetBoolean(UseSymmetricPreferenceKey,
                Build.VERSION.SdkInt >= BuildVersionCodes.M);

            // If >= API 23 we can use the KeyStore's symmetric key
            if (useSymmetric)
            {
                return GetSymmetricKey();
            }

            // NOTE: KeyStore in < API 23 can only store asymmetric keys
            // specifically, only RSA/ECB/PKCS1Padding
            // So we will wrap our symmetric AES key we just generated
            // with this and save the encrypted/wrapped key out to
            // preferences for future use.
            // ECB should be fine in this case as the AES key should be
            // contained in one block.

            var keyPair = GetAsymmetricKeyPair();
            var existingKeyString = _preferences.GetString(MasterKeyPreferenceKey, null);

            if (!string.IsNullOrEmpty(existingKeyString))
            {
                try
                {
                    var wrappedKey = Convert.FromBase64String(existingKeyString);
                    var unwrappedKey = UnwrapKey(wrappedKey, keyPair.Private);

                    return unwrappedKey.JavaCast<ISecretKey>();
                }
                catch (InvalidKeyException ikEx)
                {
                    _log.Error(ikEx, "Unable to unwrap key: Invalid Key");
                }
                catch (IllegalBlockSizeException ibsEx)
                {
                    _log.Error(ibsEx, "Unable to unwrap key: Illegal Block Size");
                }
                catch (BadPaddingException paddingEx)
                {
                    _log.Error(paddingEx, "Unable to unwrap key: Bad Padding");
                }

                _preferences.Edit().Remove(MasterKeyPreferenceKey).Commit();
            }

            var keyGenerator = KeyGenerator.GetInstance(SymmetricAlgorithm);
            var symmetricKey = keyGenerator.GenerateKey();

            var newWrappedKey = WrapKey(symmetricKey, keyPair.Public);
            _preferences.Edit().PutString(MasterKeyPreferenceKey, Convert.ToBase64String(newWrappedKey)).Commit();

            return symmetricKey;
        }

        // API 23+ Only
        private ISecretKey GetSymmetricKey()
        {
            _preferences.Edit().PutBoolean(UseSymmetricPreferenceKey, true).Commit();
            IKey existingKey;

            lock (_lock)
            {
                var keyStore = GetKeyStore();
                existingKey = keyStore.GetKey(_preferenceAlias, null);
            }

            if (existingKey != null)
            {
                return existingKey.JavaCast<ISecretKey>();
            }

            lock (_lock)
            {
#pragma warning disable CA1416
                var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KeyStoreName);

                var spec =
                    new KeyGenParameterSpec.Builder(_preferenceAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                        .SetBlockModes(KeyProperties.BlockModeGcm)
                        .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                        .SetRandomizedEncryptionRequired(false)
                        .Build();

                keyGenerator.Init(spec);
#pragma warning restore CA1416

                return keyGenerator.GenerateKey();
            }
        }

        private KeyPair GetAsymmetricKeyPair()
        {
            // set that we generated keys on pre-m device.
            _preferences.Edit().PutBoolean(UseSymmetricPreferenceKey, false).Commit();

            var asymmetricAlias = $"{_preferenceAlias}.asymmetric";

            IPrivateKey privateKey;
            IPublicKey publicKey;

            lock (_lock)
            {
                var keyStore = GetKeyStore();
                privateKey = keyStore.GetKey(asymmetricAlias, null)?.JavaCast<IPrivateKey>();
                publicKey = keyStore.GetCertificate(asymmetricAlias)?.PublicKey;
            }

            // Return the existing key if found
            if (privateKey != null && publicKey != null)
            {
                return new KeyPair(publicKey, privateKey);
            }

            var originalLocale = GetLocale();

            try
            {
                // Force to english for known bug in date parsing:
                // https://issuetracker.google.com/issues/37095309
                SetLocale(Locale.English);

                var end = DateTime.UtcNow.AddYears(20);
                var startDate = new Date();
#pragma warning disable CS0618 // Type or member is obsolete
                var endDate = new Date(end.Year, end.Month, end.Day);
#pragma warning restore CS0618 // Type or member is obsolete

                lock (_lock)
                {
                    // Otherwise we create a new key
                    var generator = KeyPairGenerator.GetInstance(AsymmetricAlgorithm, KeyStoreName);

#pragma warning disable CA1422
                    var soec = new KeyPairGeneratorSpec.Builder(_context)
                        .SetAlias(asymmetricAlias)
                        .SetSerialNumber(BigInteger.One)
                        .SetSubject(new X500Principal($"CN={asymmetricAlias} CA Certificate"))
                        .SetStartDate(startDate)
                        .SetEndDate(endDate)
                        .Build();

                    generator.Initialize(soec);
#pragma warning restore CA1422

                    return generator.GenerateKeyPair();
                }
            }
            finally
            {
                SetLocale(originalLocale);
            }
        }

        private Locale GetLocale()
        {
            var resources = _context.Resources;
            var configuration = resources.Configuration;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
#pragma warning disable CA1416
                return configuration.Locales.Get(0);
#pragma warning restore CA1416
            }

#pragma warning disable CA1422
            return configuration.Locale;
#pragma warning restore CA1422
        }

        private void SetLocale(Locale locale)
        {
            Locale.Default = locale;

            var resources = _context.Resources;
            var configuration = resources.Configuration;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                configuration.SetLocale(locale);
            }
            else
            {
#pragma warning disable CA1422
                configuration.Locale = locale;
                _context.Resources.UpdateConfiguration(configuration, _context.Resources.DisplayMetrics);
#pragma warning restore CA1422
            }
        }

        private static byte[] WrapKey(IKey keyToWrap, IKey withKey)
        {
            var cipher = Cipher.GetInstance(AsymmetricCipher);
            cipher.Init(CipherMode.WrapMode, withKey);
            return cipher.Wrap(keyToWrap);
        }

        private static IKey UnwrapKey(byte[] wrappedData, IKey withKey)
        {
            var cipher = Cipher.GetInstance(AsymmetricCipher);
            cipher.Init(CipherMode.UnwrapMode, withKey);
#pragma warning disable CA1416
            var unwrapped = cipher.Unwrap(wrappedData, KeyProperties.KeyAlgorithmAes, KeyType.SecretKey);
#pragma warning restore CA1416
            return unwrapped;
        }

        private byte[] Encrypt(string data)
        {
            var key = GetKey();

            // Generate initialization vector
            var iv = new byte[IvLength];

            var secureRandom = new SecureRandom();
            secureRandom.NextBytes(iv);

            Cipher cipher;

            // Attempt to use GCMParameterSpec by default
            try
            {
                cipher = Cipher.GetInstance(SymmetricCipher);
                cipher.Init(CipherMode.EncryptMode, key, new GCMParameterSpec(128, iv));
            }
            catch (InvalidAlgorithmParameterException)
            {
                // If we encounter this error, it's likely an old bouncycastle provider version
                // is being used which does not recognize GCMParameterSpec, but should work
                // with IvParameterSpec, however we only do this as a last effort since other
                // implementations will error if you use IvParameterSpec when GCMParameterSpec
                // is recognized and expected.
                cipher = Cipher.GetInstance(SymmetricCipher);
                cipher.Init(CipherMode.EncryptMode, key, new IvParameterSpec(iv));
            }

            var decryptedData = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = cipher.DoFinal(decryptedData);

            // Combine the IV and the encrypted data into one array
            var result = new byte[iv.Length + encryptedBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

            return result;
        }

        private string Decrypt(byte[] data)
        {
            if (data.Length < IvLength)
            {
                return null;
            }

            var key = GetKey();

            // IV will be the first 16 bytes of the encrypted data
            var iv = new byte[IvLength];
            Buffer.BlockCopy(data, 0, iv, 0, IvLength);

            Cipher cipher;

            // Attempt to use GCMParameterSpec by default
            try
            {
                cipher = Cipher.GetInstance(SymmetricCipher);
                cipher.Init(CipherMode.DecryptMode, key, new GCMParameterSpec(128, iv));
            }
            catch (InvalidAlgorithmParameterException)
            {
                // If we encounter this error, it's likely an old bouncycastle provider version
                // is being used which does not recognize GCMParameterSpec, but should work
                // with IvParameterSpec, however we only do this as a last effort since other
                // implementations will error if you use IvParameterSpec when GCMParameterSpec
                // is recognized and expected.
                cipher = Cipher.GetInstance(SymmetricCipher);
                cipher.Init(CipherMode.DecryptMode, key, new IvParameterSpec(iv));
            }

            // Decrypt starting after the first 16 bytes from the IV
            var decryptedData = cipher.DoFinal(data, IvLength, data.Length - IvLength);

            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}