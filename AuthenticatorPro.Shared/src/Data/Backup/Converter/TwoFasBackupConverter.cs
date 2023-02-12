// Copyright (C) 2023 jmh
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
    public class TwoFasBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;

        private const string BaseAlgorithm = "AES";
        private const string Mode = "GCM";
        private const string Padding = "NoPadding";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;

        private const int Iterations = 10000;
        private const int KeyLength = 32;

        public TwoFasBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var backup = JsonConvert.DeserializeObject<TwoFasBackup>(json);

            if (backup.ServicesEncrypted != null)
            {
                if (String.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Password required but not provided");
                }

                backup.Services = await Task.Run(() => DecryptServices(backup.ServicesEncrypted, password));
            }

            return ConvertBackup(backup);
        }

        private ConversionResult ConvertBackup(TwoFasBackup twoFasBackup)
        {
            var authenticators = new List<Authenticator>();
            var categories = twoFasBackup.Groups.Select(g => g.Convert()).ToList();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var service in twoFasBackup.Services)
            {
                Authenticator auth;

                try
                {
                    auth = service.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = service.Name, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);

                if (service.GroupId != null)
                {
                    var index = twoFasBackup.Groups.FindIndex(g => g.Id == service.GroupId);
                    var category = categories[index];

                    var binding = new AuthenticatorCategory(auth.Secret, category.Id);
                    bindings.Add(binding);
                }
            }

            var backup = new Backup(authenticators, categories, bindings);
            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private static List<Service> DecryptServices(string payload, string password)
        {
            var parts = payload.Split(":", 3);

            if (parts.Length < 3)
            {
                throw new ArgumentException("Invalid payload format");
            }

            var encryptedData = Convert.FromBase64String(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var iv = Convert.FromBase64String(parts[2]);

            var key = DeriveKey(password, salt);
            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            var decryptedBytes = cipher.DoFinal(encryptedData);
            var decryptedJson = Encoding.UTF8.GetString(decryptedBytes);

            return JsonConvert.DeserializeObject<List<Service>>(decryptedJson);
        }

        private static KeyParameter DeriveKey(string password, byte[] salt)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var generator = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            generator.Init(passwordBytes, salt, Iterations);
            return (KeyParameter) generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
        }

        private class TwoFasBackup
        {
            [JsonProperty(PropertyName = "services")]
            public List<Service> Services { get; set; }

            [JsonProperty(PropertyName = "groups")]
            public List<Group> Groups { get; set; }

            [JsonProperty(PropertyName = "servicesEncrypted")]
            public string ServicesEncrypted { get; set; }
        }

        private class Service
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "secret")]
            public string Secret { get; set; }

            [JsonProperty(PropertyName = "otp")]
            public Otp Otp { get; set; }

            [JsonProperty(PropertyName = "groupId")]
            public string GroupId { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                var type = Otp.TokenType switch
                {
                    "TOTP" => AuthenticatorType.Totp,
                    "HOTP" => AuthenticatorType.Hotp,
                    _ => throw new ArgumentException($"Type '{Otp.TokenType}' not supported")
                };

                var algorithm = Otp.Algorithm switch
                {
                    "SHA1" => HashAlgorithm.Sha1,
                    "SHA256" => HashAlgorithm.Sha256,
                    "SHA512" => HashAlgorithm.Sha512,
                    _ => throw new ArgumentException($"Algorithm '{Otp.Algorithm}' not supported")
                };

                string issuer;
                string username;

                if (String.IsNullOrEmpty(Otp.Issuer))
                {
                    issuer = !String.IsNullOrEmpty(Otp.Account) ? Otp.Account : Name;
                    username = null;
                }
                else
                {
                    issuer = Otp.Issuer;
                    username = Otp.Account;
                }

                return new Authenticator
                {
                    Secret = Authenticator.CleanSecret(Secret, type),
                    Issuer = issuer,
                    Username = username,
                    Digits = Otp.Digits,
                    Period = Otp.Period,
                    Counter = Otp.Counter,
                    Type = type,
                    Algorithm = algorithm,
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }

        private class Otp
        {
            [JsonProperty(PropertyName = "account")]
            public string Account { get; set; }

            [JsonProperty(PropertyName = "issuer")]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public int Digits { get; set; }

            [JsonProperty(PropertyName = "period")]
            public int Period { get; set; }

            [JsonProperty(PropertyName = "algorithm")]
            public string Algorithm { get; set; }

            [JsonProperty(PropertyName = "counter")]
            public int Counter { get; set; }

            [JsonProperty(PropertyName = "tokenType")]
            public string TokenType { get; set; }
        }

        private class Group
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            public Category Convert()
            {
                return new Category(Name);
            }
        }
    }
}