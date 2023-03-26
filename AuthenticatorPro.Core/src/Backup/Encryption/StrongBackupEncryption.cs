// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Konscious.Security.Cryptography;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Backup.Encryption
{
    public class StrongBackupEncryption : IBackupEncryption
    {
        private const string Header = "AUTHENTICATORPRO";

        // Key derivation
        private const int Parallelism = 4;
        private const int Iterations = 3;
        private const int MemorySize = 65536;

        private const int SaltLength = 16;
        private const int KeyLength = 32;

        // Encryption
        private const string BaseAlgorithm = "AES";
        private const string Mode = "GCM";
        private const string Padding = "NoPadding";
        private const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;
        private const int IvLength = 12;
        private const int TagLength = 32;

        private static async Task<byte[]> DeriveKeyAsync(string password, byte[] salt)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            var argon2 = new Argon2id(passwordBytes);
            argon2.DegreeOfParallelism = Parallelism;
            argon2.Iterations = Iterations;
            argon2.MemorySize = MemorySize;
            argon2.Salt = salt;

            return await argon2.GetBytesAsync(KeyLength);
        }

        public async Task<byte[]> EncryptAsync(Backup backup, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Cannot encrypt without a password");
            }

            var random = new SecureRandom();
            var salt = random.GenerateSeed(SaltLength);
            var iv = random.GenerateSeed(IvLength);
            var tag = random.GenerateSeed(TagLength);

            var key = await DeriveKeyAsync(password, salt);
            var parameters = new AeadParameters(new KeyParameter(key), TagLength, tag, iv);

            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(true, parameters);

            var json = JsonConvert.SerializeObject(backup);
            var unencryptedData = Encoding.UTF8.GetBytes(json);
            var encryptedData = await Task.Run(() => cipher.DoFinal(unencryptedData));

            var headerBytes = Encoding.UTF8.GetBytes(Header);
            var output = new byte[Header.Length + SaltLength + IvLength + encryptedData.Length + TagLength];

            Buffer.BlockCopy(headerBytes, 0, output, 0, headerBytes.Length);
            Buffer.BlockCopy(salt, 0, output, headerBytes.Length, SaltLength);
            Buffer.BlockCopy(iv, 0, output, headerBytes.Length + SaltLength, IvLength);
            Buffer.BlockCopy(encryptedData, 0, output, headerBytes.Length + SaltLength + IvLength, encryptedData.Length);
            Buffer.BlockCopy(tag, 0, output, headerBytes.Length + SaltLength + IvLength + encryptedData.Length, TagLength);

            return output;
        }

        public async Task<Backup> DecryptAsync(byte[] data, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Cannot decrypt without a password");
            }

            if (!HasValidEncryptionHeader(data))
            {
                throw new ArgumentException("Header does not match");
            }

            var salt = data.Skip(Header.Length).Take(SaltLength).ToArray();
            var key = await DeriveKeyAsync(password, salt);
            var iv = data.Skip(Header.Length).Skip(SaltLength).Take(IvLength).ToArray();

            var authenticatedData = data.Skip(Header.Length + SaltLength + IvLength)
                .Take(data.Length - Header.Length - SaltLength - IvLength).ToArray();

            var encryptedData = authenticatedData[..^TagLength];
            var tag = authenticatedData[^TagLength..authenticatedData.Length];

            var parameters = new AeadParameters(new KeyParameter(key), TagLength, tag, iv);

            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, parameters);

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

        private static bool HasValidEncryptionHeader(IEnumerable<byte> data)
        {
            var foundHeader = data.Take(Header.Length).ToArray();
            var headerBytes = Encoding.UTF8.GetBytes(Header);
            return headerBytes.SequenceEqual(foundHeader);
        }
    }
}