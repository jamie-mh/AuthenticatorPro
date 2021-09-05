// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using SimpleBase;
using System;
using System.Globalization;
using System.Security.Cryptography;

namespace AuthenticatorPro.Shared.Data.Generator
{
    public abstract class HmacOtp : IDisposable
    {
        private readonly HMAC _hmac;
        private readonly int _digits;

        protected HmacOtp(string secret, HashAlgorithm algorithm, int digits)
        {
            _digits = digits;

            var secretBytes = Base32.Rfc4648.Decode(secret).ToArray();
            _hmac = algorithm switch
            {
                HashAlgorithm.Sha1 => new HMACSHA1(secretBytes),
                HashAlgorithm.Sha256 => new HMACSHA256(secretBytes),
                HashAlgorithm.Sha512 => new HMACSHA512(secretBytes),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
            };
        }

        protected int Compute(byte[] counter)
        {
            var hash = _hmac.ComputeHash(counter);
            var offset = hash[^1] & 0xF;

            return ((hash[offset] & 0x7F) << 24) |
                   ((hash[offset + 1] & 0xFF) << 16) |
                   ((hash[offset + 2] & 0xFF) << 8) |
                   ((hash[offset + 3] & 0xFF) << 0);
        }

        protected string Truncate(int material)
        {
            var otp = material % Math.Pow(10, _digits);
            var code = otp.ToString(CultureInfo.InvariantCulture).PadLeft(_digits, '0');

            return code;
        }

        public void Dispose()
        {
            _hmac?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}