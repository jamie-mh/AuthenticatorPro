// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuthenticatorPro.Shared.Data.Backup
{
    public class Backup
    {
        public const string FileExtension = "authpro";
        public const string MimeType = "application/octet-stream";

        private const string Header = "AuthenticatorPro";

        private const string BaseAlgorithm = "AES";
        private const string Mode = "CBC";
        private const string Padding = "PKCS7";
        private const string Algorithm = BaseAlgorithm + "/" + Mode + "/" + Padding;

        private const int Iterations = 64000;
        private const int KeyLength = 32;
        private const int IvLength = 16;
        private const int SaltLength = 20;

        public IEnumerable<Authenticator> Authenticators { get; }
        public IEnumerable<Category> Categories { get; }
        public IEnumerable<AuthenticatorCategory> AuthenticatorCategories { get; }
        public IEnumerable<CustomIcon> CustomIcons { get; }

        public Backup(IEnumerable<Authenticator> authenticators, IEnumerable<Category> categories = null,
            IEnumerable<AuthenticatorCategory> authenticatorCategories = null,
            IEnumerable<CustomIcon> customIcons = null)
        {
            Authenticators = authenticators ??
                             throw new ArgumentNullException(nameof(authenticators),
                                 "Backup must contain authenticators");
            Categories = categories;
            AuthenticatorCategories = authenticatorCategories;
            CustomIcons = customIcons;
        }

        private static KeyParameter DerivePassword(string password, byte[] salt)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var generator = new Pkcs5S2ParametersGenerator(new Sha1Digest());
            generator.Init(passwordBytes, salt, Iterations);
            return (KeyParameter) generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
        }

        public byte[] ToBytes(string password)
        {
            var json = JsonConvert.SerializeObject(this);

            if (String.IsNullOrEmpty(password))
            {
                return Encoding.UTF8.GetBytes(json);
            }

            var random = new SecureRandom();
            var iv = random.GenerateSeed(IvLength);
            var salt = random.GenerateSeed(SaltLength);

            var key = DerivePassword(password, salt);

            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(Algorithm);
            cipher.Init(true, keyParameter);

            var unencryptedData = Encoding.UTF8.GetBytes(json);
            var encryptedData = cipher.DoFinal(unencryptedData);

            var headerBytes = Encoding.UTF8.GetBytes(Header);
            var output = new byte[Header.Length + SaltLength + IvLength + encryptedData.Length];

            Buffer.BlockCopy(headerBytes, 0, output, 0, headerBytes.Length);
            Buffer.BlockCopy(salt, 0, output, headerBytes.Length, SaltLength);
            Buffer.BlockCopy(iv, 0, output, headerBytes.Length + SaltLength, IvLength);
            Buffer.BlockCopy(encryptedData, 0, output, headerBytes.Length + SaltLength + IvLength,
                encryptedData.Length);

            return output;
        }

        public static Backup FromBytes(byte[] data, string password)
        {
            string json;

            if (String.IsNullOrEmpty(password))
            {
                json = Encoding.UTF8.GetString(data);
            }
            else
            {
                var foundHeader = data.Take(Header.Length).ToArray();
                var headerBytes = Encoding.UTF8.GetBytes(Header);

                if (!headerBytes.SequenceEqual(foundHeader))
                {
                    throw new ArgumentException("Header does not match");
                }

                var salt = data.Skip(Header.Length).Take(SaltLength).ToArray();
                var key = DerivePassword(password, salt);

                var iv = data.Skip(Header.Length).Skip(SaltLength).Take(IvLength).ToArray();
                var encryptedData = data.Skip(Header.Length + SaltLength + IvLength)
                    .Take(data.Length - Header.Length - SaltLength - IvLength).ToArray();

                var keyParameter = new ParametersWithIV(key, iv);
                var cipher = CipherUtilities.GetCipher(Algorithm);
                cipher.Init(false, keyParameter);

                var unencryptedData = cipher.DoFinal(encryptedData);
                json = Encoding.UTF8.GetString(unencryptedData);
            }

            try
            {
                return JsonConvert.DeserializeObject<Backup>(json);
            }
            catch (JsonException e)
            {
                throw new ArgumentException("File invalid", e);
            }
        }

        public static bool IsReadableWithoutPassword(byte[] data)
        {
            if (data[0] != '{' || data[^1] != '}')
            {
                return false;
            }

            try
            {
                var json = Encoding.UTF8.GetString(data);
                _ = JObject.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}