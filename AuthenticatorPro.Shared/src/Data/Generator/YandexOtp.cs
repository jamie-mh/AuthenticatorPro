// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using SimpleBase;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public class YandexOtp : IGenerator, IDisposable
    {
        public const int Digits = 8;
        public const int SecretByteCount = 16;
        private const int Period = 30;

        private readonly HMAC _hmac;

        public YandexOtp(string secret)
        {
            var secretBytes = Base32.Rfc4648.Decode(secret).ToArray();

            using var sha256 = new SHA256Managed();
            var key = sha256.ComputeHash(secretBytes);

            if (key[0] == 0)
            {
                key = key.Skip(1).ToArray();
            }

            _hmac = new HMACSHA256(key);
        }

        public static string GetCombinedSecretPin(string secret, string pin)
        {
            var pinBytes = Encoding.UTF8.GetBytes(pin);
            var secretBytes = Base32.Rfc4648.Decode(secret).ToArray();

            if (secretBytes.Length < SecretByteCount)
            {
                throw new ArgumentException("Secret too short");
            }

            var combined = new byte[pinBytes.Length + SecretByteCount];
            Buffer.BlockCopy(pinBytes, 0, combined, 0, pinBytes.Length);
            Buffer.BlockCopy(secretBytes, 0, combined, pinBytes.Length, SecretByteCount);

            return Base32.Rfc4648.Encode(combined);
        }

        private static long ComputeMaterial(byte[] hash)
        {
            var offset = hash.Last() & 15;
            var bytes = hash.Skip(offset).Take(8).ToArray();
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes) & long.MaxValue;
        }

        public string Compute(long counter)
        {
            var counterBytes = Totp.GetCounterBytes(counter, Period);
            var hash = _hmac.ComputeHash(counterBytes);

            var material = ComputeMaterial(hash);
            var truncated = material % (long) Math.Pow(26, Digits);

            return Finalise(truncated);
        }

        private static string Finalise(long material)
        {
            var result = new char[Digits];

            for (var i = Digits - 1; i >= 0; --i)
            {
                result[i] = (char) ('a' + material % 26);
                material /= 26;
            }

            return new String(result);
        }

        public void Dispose()
        {
            _hmac?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}