// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace AuthenticatorPro.Core.Backup.Encryption
{
    public class LegacyBackupEncryption : IBackupEncryption
    {
        private const string Header = "AuthenticatorPro";

        // Key derivation
        private const int Iterations = 64000;
        private const int SaltLength = 20;
        private const int KeyLength = 32;

        // Encryption
        private const string BaseAlgorithm = "AES";
        private const string Mode = "CBC";
        private const string Padding = "PKCS7";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;
        private const int IvLength = 16;

        public async Task<byte[]> EncryptAsync(Backup backup, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Cannot encrypt without a password");
            }

            var random = new SecureRandom();
            var iv = random.GenerateSeed(IvLength);
            var salt = random.GenerateSeed(SaltLength);

            var key = await DeriveKeyAsync(password, salt);

            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(true, keyParameter);

            var json = JsonConvert.SerializeObject(backup);
            var unencryptedData = Encoding.UTF8.GetBytes(json);
            var encryptedData = await Task.Run(() => cipher.DoFinal(unencryptedData));

            var headerBytes = Encoding.UTF8.GetBytes(Header);
            var output = new byte[Header.Length + SaltLength + IvLength + encryptedData.Length];

            Buffer.BlockCopy(headerBytes, 0, output, 0, headerBytes.Length);
            Buffer.BlockCopy(salt, 0, output, headerBytes.Length, SaltLength);
            Buffer.BlockCopy(iv, 0, output, headerBytes.Length + SaltLength, IvLength);
            Buffer.BlockCopy(encryptedData, 0, output, headerBytes.Length + SaltLength + IvLength,
                encryptedData.Length);

            return output;
        }

        public async Task<Backup> DecryptAsync(byte[] data, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Cannot decrypt without a password");
            }

            if (!CanBeDecrypted(data))
            {
                throw new ArgumentException("Header does not match");
            }

            await using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            reader.ReadBytes(Header.Length);

            var salt = reader.ReadBytes(SaltLength);
            var iv = reader.ReadBytes(IvLength);
            var encryptedData = reader.ReadBytes(data.Length - Header.Length - SaltLength - IvLength);

            var key = await DeriveKeyAsync(password, salt);
            var keyParameter = new ParametersWithIV(key, iv);

            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            byte[] unencryptedData;

            try
            {
                unencryptedData = await Task.Run(() => cipher.DoFinal(encryptedData));
            }
            catch (InvalidCipherTextException e)
            {
                throw new ArgumentException("Invalid password", e);
            }

            var json = Encoding.UTF8.GetString(unencryptedData);
            return JsonConvert.DeserializeObject<Backup>(json);
        }

        public bool CanBeDecrypted(byte[] data)
        {
            var foundHeader = data.Take(Header.Length).ToArray();
            var headerBytes = Encoding.UTF8.GetBytes(Header);
            return headerBytes.SequenceEqual(foundHeader);
        }

        private static Task<KeyParameter> DeriveKeyAsync(string password, byte[] salt)
        {
            return Task.Run(delegate
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var generator = new Pkcs5S2ParametersGenerator(new Sha1Digest());
                generator.Init(passwordBytes, salt, Iterations);
                return (KeyParameter) generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
            });
        }
    }
}