// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Util;
using Konscious.Security.Cryptography;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace AuthenticatorPro.Core.Converter
{
    public class BitwardenBackupConverter : BackupConverter
    {
        private const string BaseAlgorithm = "AES";
        private const string Mode = "CBC";
        private const string Padding = "PKCS7";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;

        private const int KeyLength = 32;
        private const int LoginType = 1;

        public BitwardenBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var export = JsonConvert.DeserializeObject<Export>(json);

            Vault vault;

            if (export.Encrypted)
            {
                if (!export.PasswordProtected)
                {
                    throw new ArgumentException("Cannot decrypt account restricted backup");
                }

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Cannot decrypt without a password");
                }

                var encryption = JsonConvert.DeserializeObject<Encryption>(json);
                var key = await DeriveKeyAsync(encryption, password);
                vault = await Task.Run(() => Decrypt(encryption, key));
            }
            else
            {
                vault = JsonConvert.DeserializeObject<Vault>(json);
            }

            return ConvertVault(vault);
        }

        private static Vault Decrypt(Encryption encryption, byte[] key)
        {
            var parts = encryption.Data.Split(".", 2);
            var data = parts[1].Split("|");

            if (data.Length < 3)
            {
                throw new ArgumentException("Missing parts in encrypted data");
            }

            var iv = Convert.FromBase64String(data[0]);
            var payload = Convert.FromBase64String(data[1]);
            var mac = Convert.FromBase64String(data[2]);

            var encryptionKey = HkdfExpand(key, "enc");
            var macKey = HkdfExpand(key, "mac");

            if (!VerifyMac(macKey, iv, payload, mac))
            {
                throw new ArgumentException("The password is incorrect. Invalid HMAC.");
            }

            var keyParameter = new ParametersWithIV(new KeyParameter(encryptionKey), iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            byte[] decryptedBytes;

            try
            {
                decryptedBytes = cipher.DoFinal(payload);
            }
            catch (InvalidCipherTextException e)
            {
                throw new ArgumentException("The password is incorrect. Bad cipher text.", e);
            }

            var json = Encoding.UTF8.GetString(decryptedBytes);
            return JsonConvert.DeserializeObject<Vault>(json);
        }

        private static bool VerifyMac(byte[] key, byte[] iv, byte[] payload, byte[] expected)
        {
            var material = new byte[iv.Length + payload.Length];
            Buffer.BlockCopy(iv, 0, material, 0, iv.Length);
            Buffer.BlockCopy(payload, 0, material, iv.Length, payload.Length);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(material);

            return hash.SequenceEqual(expected);
        }

        private static async Task<byte[]> DeriveKeyAsync(Encryption encryption, string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            // Salt is base 64 but not decoded for some reason
            var saltBytes = Encoding.UTF8.GetBytes(encryption.Salt);

            switch (encryption.KdfType)
            {
                case Encryption.Kdf.Pbkdf2Sha256:
                    return await Task.Run(() => DerivePbkdf2Sha256(passwordBytes, encryption.KdfIterations, saltBytes));

                case Encryption.Kdf.Argon2Id:
                {
                    if (encryption.KdfMemory == null || encryption.KdfParallelism == null)
                    {
                        throw new ArgumentException("Missing memory and/or parallelism parameters");
                    }

                    return await DeriveArgon2IdAsync(passwordBytes, encryption.KdfIterations,
                        encryption.KdfMemory.Value,
                        encryption.KdfParallelism.Value, saltBytes);
                }

                default:
                    throw new ArgumentException("Unsupported KDF type");
            }
        }

        private static byte[] DerivePbkdf2Sha256(byte[] password, int iterations, byte[] salt)
        {
            var generator = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            generator.Init(password, salt, iterations);
            var parameter = (KeyParameter) generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
            return parameter.GetKey();
        }

        private static async Task<byte[]> DeriveArgon2IdAsync(
            byte[] password, int iterations, int memory, int parallelism, byte[] salt)
        {
            var argon2 = new Argon2id(password);
            argon2.DegreeOfParallelism = parallelism;
            argon2.Iterations = iterations;
            argon2.MemorySize = memory * 1024;
            argon2.Salt = SHA256.HashData(salt);

            return await argon2.GetBytesAsync(KeyLength);
        }

        private static byte[] HkdfExpand(byte[] key, string info)
        {
            var generator = new HkdfBytesGenerator(new Sha256Digest());
            var infoBytes = Encoding.UTF8.GetBytes(info);
            generator.Init(HkdfParameters.SkipExtractParameters(key, infoBytes));

            var output = new byte[KeyLength];
            generator.GenerateBytes(output, 0, KeyLength);

            return output;
        }

        private ConversionResult ConvertVault(Vault vault)
        {
            var convertableItems = vault.Items.Where(item =>
                item.Type == LoginType && item.Login != null && !string.IsNullOrEmpty(item.Login.Totp));

            var authenticators = new List<Authenticator>();
            var categories = vault.Folders.Select(f => f.Convert()).ToList();
            var bindings = new List<AuthenticatorCategory>();
            var failures = new List<ConversionFailure>();

            foreach (var item in convertableItems)
            {
                Authenticator auth;

                try
                {
                    auth = item.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = item.Name, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);

                if (item.FolderId != null)
                {
                    var folderName = vault.Folders.First(f => f.Id == item.FolderId).Name;
                    var category = categories.First(c => c.Name == folderName);

                    bindings.Add(new AuthenticatorCategory(auth.Secret, category.Id));
                }
            }

            var backup = new Backup.Backup
            {
                Authenticators = authenticators, Categories = categories, AuthenticatorCategories = bindings
            };

            return new ConversionResult { Failures = failures, Backup = backup };
        }

        private sealed class Export
        {
            [JsonProperty(PropertyName = "encrypted")]
            public bool Encrypted { get; set; }

            [JsonProperty(PropertyName = "passwordProtected")]
            public bool PasswordProtected { get; set; }
        }

        private sealed class Encryption
        {
            public enum Kdf
            {
                Pbkdf2Sha256 = 0,
                Argon2Id = 1
            }

            [JsonProperty(PropertyName = "salt")]
            public string Salt { get; set; }

            [JsonProperty(PropertyName = "kdfType")]
            public Kdf KdfType { get; set; }

            [JsonProperty(PropertyName = "kdfIterations")]
            public int KdfIterations { get; set; }

            [JsonProperty(PropertyName = "kdfMemory")]
            public int? KdfMemory { get; set; }

            [JsonProperty(PropertyName = "kdfParallelism")]
            public int? KdfParallelism { get; set; }

            [JsonProperty(PropertyName = "encKeyValidation_DO_NOT_EDIT")]
            public string EncKeyValidation { get; set; }

            [JsonProperty(PropertyName = "data")]
            public string Data { get; set; }
        }

        private sealed class Vault
        {
            [JsonProperty(PropertyName = "folders")]
            public List<Folder> Folders { get; set; }

            [JsonProperty(PropertyName = "items")]
            public List<Item> Items { get; set; }
        }

        private sealed class Folder
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

        private sealed class Item
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "folderId")]
            public string FolderId { get; set; }

            [JsonProperty(PropertyName = "type")]
            public int Type { get; set; }

            [JsonProperty(PropertyName = "login")]
            public Login Login { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                Authenticator ConvertFromInfo(AuthenticatorType type, string secret)
                {
                    return new Authenticator
                    {
                        Issuer = Name.Truncate(Authenticator.IssuerMaxLength),
                        Username = Login.Username.Truncate(Authenticator.UsernameMaxLength),
                        Type = type,
                        Algorithm = Authenticator.DefaultAlgorithm,
                        Digits = type.GetDefaultDigits(),
                        Period = type.GetDefaultPeriod(),
                        Icon = iconResolver.FindServiceKeyByName(Name),
                        Secret = SecretUtil.Clean(secret, type)
                    };
                }

                if (Login.Totp.StartsWith("otpauth"))
                {
                    return UriParser.ParseStandardUri(Login.Totp, iconResolver).Authenticator;
                }

                if (Login.Totp.StartsWith("steam"))
                {
                    var secret = Login.Totp["steam://".Length..];
                    return ConvertFromInfo(AuthenticatorType.SteamOtp, secret);
                }

                return ConvertFromInfo(AuthenticatorType.Totp, Login.Totp);
            }
        }

        private sealed class Login
        {
            [JsonProperty(PropertyName = "username")]
            public string Username { get; set; }

            [JsonProperty(PropertyName = "totp")]
            public string Totp { get; set; }
        }
    }
}