// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Entity;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Data.Backup.Converter
{
    public class AndOtpBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;

        private const string BaseAlgorithm = "AES";
        private const string Mode = "GCM";
        private const string Padding = "NoPadding";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;

        private const int IterationsLength = 4;
        private const int SaltLength = 12;
        private const int IvLength = 12;
        private const int KeyLength = 32;


        public AndOtpBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            string json;

            if (String.IsNullOrEmpty(password))
            {
                json = Encoding.UTF8.GetString(data);
            }
            else
            {
                json = await Task.Run(() => Decrypt(data, password));
            }

            var sourceAccounts = JsonConvert.DeserializeObject<List<Account>>(json);

            var authenticators = new List<Authenticator>();
            var categories = new List<Category>();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var account in sourceAccounts)
            {
                Authenticator auth;

                try
                {
                    auth = account.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure
                    {
                        Description = account.Issuer,
                        Error = e.Message
                    });

                    continue;
                }

                authenticators.Add(auth);

                foreach (var tag in account.Tags)
                {
                    var category = categories.FirstOrDefault(c => c.Name == tag);

                    if (category == null)
                    {
                        category = new Category(tag);
                        categories.Add(category);
                    }

                    var binding = new AuthenticatorCategory(auth.Secret, category.Id);
                    bindings.Add(binding);
                }
            }

            var backup = new Backup(authenticators, categories, bindings);
            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private static KeyParameter DeriveKey(string password, byte[] salt, int iterations)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var generator = new Pkcs5S2ParametersGenerator(new Sha1Digest());
            generator.Init(passwordBytes, salt, iterations);
            return (KeyParameter) generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
        }

        private static string Decrypt(byte[] data, string password)
        {
            var iterationsBytes = data.Take(IterationsLength);

            if (BitConverter.IsLittleEndian)
            {
                iterationsBytes = iterationsBytes.Reverse();
            }

            var iterations = (int) BitConverter.ToUInt32(iterationsBytes.ToArray());

            var salt = data.Skip(IterationsLength).Take(SaltLength).ToArray();
            var iv = data.Skip(IterationsLength + SaltLength).Take(IvLength).ToArray();
            var payload = data.Skip(IterationsLength + SaltLength + IvLength).ToArray();

            var key = DeriveKey(password, salt, iterations);

            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            var decrypted = cipher.DoFinal(payload);
            return Encoding.UTF8.GetString(decrypted);
        }

        private class Account
        {
            [JsonProperty(PropertyName = "secret")]
            public string Secret { get; set; }

            [JsonProperty(PropertyName = "issuer")]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = "label")] public string Label { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; }

            [JsonProperty(PropertyName = "type")] public string Type { get; set; }

            [JsonProperty(PropertyName = "algorithm")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "thumbnail")]
            public string Thumbnail { get; set; }

            [JsonProperty(PropertyName = "period")]
            public int? Period { get; set; }

            [JsonProperty(PropertyName = "counter")]
            public int Counter { get; set; }

            [JsonProperty(PropertyName = "tags")] public List<string> Tags { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Type switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    "STEAM" => AuthenticatorType.SteamOtp,
                    _ => throw new ArgumentException($"Type '{Type}' not supported")
                };

                var algorithm = Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException($"Algorithm '{Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (String.IsNullOrEmpty(Issuer))
                {
                    issuer = Label;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Label;
                }

                return new Authenticator
                {
                    Secret = Authenticator.CleanSecret(Secret, type),
                    Issuer = issuer,
                    Username = username,
                    Digits = Digits,
                    Period = Period ?? type.GetDefaultPeriod(),
                    Counter = Counter,
                    Type = type,
                    Algorithm = algorithm,
                    Icon = iconResolver.FindServiceKeyByName(Thumbnail)
                };
            }
        }
    }
}