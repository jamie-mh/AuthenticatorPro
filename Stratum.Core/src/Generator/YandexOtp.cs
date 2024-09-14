// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SimpleBase;

namespace Stratum.Core.Generator
{
    public class YandexOtp : IGenerator, IDisposable
    {
        public const int Digits = 8;
        public const int SecretByteCount = 16;
        private const int Period = 30;

        private readonly HMAC _hmac;
        private bool _isDisposed;

        public YandexOtp(string secret, string pin)
        {
            var secretBytes = GetSecretBytes(secret, pin);
            var key = SHA256.HashData(secretBytes);

            if (key[0] == 0)
            {
                key = key.Skip(1).ToArray();
            }

            _hmac = new HMACSHA256(key);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string Compute(long counter)
        {
            var counterBytes = Totp.GetCounterBytes(counter, Period);
            var hash = _hmac.ComputeHash(counterBytes);

            var material = ComputeMaterial(hash);
            var truncated = material % (long) Math.Pow(26, Digits);

            return Finalise(truncated);
        }

        private static byte[] GetSecretBytes(string secret, string pin)
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

            return combined;
        }

        private static long ComputeMaterial(byte[] hash)
        {
            var offset = hash.Last() & 15;
            var bytes = hash.Skip(offset).Take(8).ToArray();
            return BinaryPrimitives.ReadInt64BigEndian(bytes) & long.MaxValue;
        }

        private static string Finalise(long material)
        {
            var result = new char[Digits];

            for (var i = Digits - 1; i >= 0; --i)
            {
                result[i] = (char) ('a' + material % 26);
                material /= 26;
            }

            return new string(result);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _hmac?.Dispose();
            }

            _isDisposed = true;
        }
    }
}