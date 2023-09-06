// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SimpleBase;

namespace AuthenticatorPro.Core.Converter
{
    public partial class FreeOtpBackupConverter : BackupConverter
    {
        private const int MasterKeyBytes = 32;

        public FreeOtpBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;

        [GeneratedRegex("([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?:-token)?|masterKey)[tx]")]
        private static partial Regex MapKeyRegex();

        [GeneratedRegex("({.*?})[tx]")]
        private static partial Regex MapValueRegex();

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            if (password == null)
            {
                throw new ArgumentException("Password cannot be null");
            }

            var decoded = Encoding.UTF8.GetString(data);
            var values = DeserialiseApproximately(decoded);

            return await Task.Run(() => DecryptAndConvert(values, password));
        }

        private ConversionResult DecryptAndConvert(Dictionary<string, string> values, string password)
        {
            var masterKeyInfo = JsonConvert.DeserializeObject<MasterKey>(values["masterKey"]);
            var masterKey = DecryptMasterKey(masterKeyInfo, password);

            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var (key, value) in values)
            {
                if (!key.EndsWith("-token"))
                {
                    continue;
                }

                var info = JsonConvert.DeserializeObject<TokenInfo>(value);

                var keyJson = values[key.Replace("-token", "")];
                var keyInfo = JsonConvert.DeserializeObject<TokenKeyInfo>(keyJson);
                var encryptedKey = JsonConvert.DeserializeObject<EncryptedKey>(keyInfo.Key);
                var secret = DecryptEncryptedKey(encryptedKey, masterKey);

                Authenticator auth;

                try
                {
                    auth = info.Convert(IconResolver, secret);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = info.Label, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private static KeyParameter DecryptMasterKey(MasterKey masterKey, string password)
        {
            var salt = MemoryMarshal.Cast<sbyte, byte>(masterKey.Salt).ToArray();
            var key = DeriveKey(password, masterKey.Algorithm, masterKey.Iterations, salt);
            var master = DecryptEncryptedKey(masterKey.EncryptedKey, key);

            return new KeyParameter(master);
        }

        private static byte[] DecryptEncryptedKey(EncryptedKey encryptedKey, KeyParameter key)
        {
            var encodedParams = MemoryMarshal.Cast<sbyte, byte>(encryptedKey.Parameters).ToArray();
            var encryptedData = MemoryMarshal.Cast<sbyte, byte>(encryptedKey.CipherText).ToArray();

            var parameters = ReadAsn1Parameters(key, encodedParams, encryptedKey.Token);

            var cipher = CipherUtilities.GetCipher(encryptedKey.Cipher);
            cipher.Init(false, parameters);

            try
            {
                return cipher.DoFinal(encryptedData);
            }
            catch (InvalidCipherTextException e)
            {
                throw new ArgumentException("Invalid password", e);
            }
        }

        private static AeadParameters ReadAsn1Parameters(KeyParameter key, byte[] encodedParameters,
            string associatedText)
        {
            var sequence = (DerSequence) Asn1Sequence.FromByteArray(encodedParameters);
            var parts = sequence.ToArray();

            var iv = (DerOctetString) parts[0];
            var macLength = (DerInteger) parts[1];
            var associatedBytes = Encoding.UTF8.GetBytes(associatedText);

            return new AeadParameters(key, macLength.IntValueExact * 8, iv.GetOctets(), associatedBytes);
        }

        private static KeyParameter DeriveKey(string password, string algorithm, int iterations, byte[] salt)
        {
            IDigest digest = algorithm switch
            {
                "PBKDF2withHmacSHA1" => new Sha1Digest(),
                "PBKDF2withHmacSHA512" => new Sha512Digest(),
                _ => throw new ArgumentException($"Unsupported algorithm '{algorithm}'")
            };

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var generator = new Pkcs5S2ParametersGenerator(digest);
            generator.Init(passwordBytes, salt, iterations);

            return (KeyParameter) generator.GenerateDerivedParameters("AES", MasterKeyBytes * 8);
        }

        // Use regex to deserialise the Java object binary serialisation.
        // Since there is only a HashMap object, there is no need to write a proper deserialiser
        private static Dictionary<string, string> DeserialiseApproximately(string data)
        {
            var keys = MapKeyRegex().Matches(data);
            var values = MapValueRegex().Matches(data);
            var result = new Dictionary<string, string>();

            foreach (var (key, value) in keys.Zip(values))
            {
                result.Add(key.Groups[1].Value, value.Groups[1].Value);
            }

            return result;
        }

        private sealed class MasterKey
        {
            [JsonProperty(PropertyName = "mAlgorithm")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "mIterations")]
            public int Iterations { get; set; }

            [JsonProperty(PropertyName = "mSalt")]
            public sbyte[] Salt { get; set; }

            [JsonProperty(PropertyName = "mEncryptedKey")]
            public EncryptedKey EncryptedKey { get; set; }
        }

        private sealed class EncryptedKey
        {
            [JsonProperty(PropertyName = "mCipher")]
            public string Cipher { get; set; }

            [JsonProperty(PropertyName = "mCipherText")]
            public sbyte[] CipherText { get; set; }

            [JsonProperty(PropertyName = "mParameters")]
            public sbyte[] Parameters { get; set; }

            [JsonProperty(PropertyName = "mToken")]
            public string Token { get; set; }
        }

        private sealed class TokenInfo
        {
            [JsonProperty(PropertyName = "issuerExt")]
            public string IssuerExt { get; set; }

            [JsonProperty(PropertyName = "issuerInt")]
            public string IssuerInt { get; set; }

            [JsonProperty(PropertyName = "label")]
            public string Label { get; set; }

            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "algo")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "counter")]
            public int Counter { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; } = AuthenticatorType.Totp.GetDefaultDigits();

            [JsonProperty(PropertyName = "period")]
            public int Period { get; set; } = AuthenticatorType.Totp.GetDefaultPeriod();

            public Authenticator Convert(IIconResolver iconResolver, byte[] secret)
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    _ => throw new ArgumentException($"Type '{Type}' not supported")
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" or null => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException($"Algorithm '{Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (IssuerExt == null)
                {
                    issuer = Label;
                    username = null;
                }
                else
                {
                    issuer = IssuerExt;
                    username = Label;
                }

                return new Authenticator
                {
                    Issuer = issuer.Truncate(Authenticator.IssuerMaxLength),
                    Username = username.Truncate(Authenticator.UsernameMaxLength),
                    Algorithm = algorithm,
                    Type = type,
                    Counter = Counter,
                    Digits = Digits,
                    Icon = iconResolver.FindServiceKeyByName(issuer),
                    Period = Period,
                    Secret = Base32.Rfc4648.Encode(secret)
                };
            }
        }

        private sealed class TokenKeyInfo
        {
            [JsonProperty(PropertyName = "key")]
            public string Key { get; set; }
        }
    }
}