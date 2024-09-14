// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Text;

namespace Stratum.Core.Generator
{
    public class SteamOtp : Totp
    {
        public const int Digits = 5;
        private const int Period = 30;
        private const HashAlgorithm Algorithm = HashAlgorithm.Sha1;
        private const string Alphabet = "23456789BCDFGHJKMNPQRTVWXY";

        public SteamOtp(string secret) : base(secret, Period, Algorithm, Digits)
        {
        }

        protected override string Finalise(int material)
        {
            var builder = new StringBuilder(Digits);

            for (var i = 0; i < Digits; i++)
            {
                builder.Append(Alphabet[material % Alphabet.Length]);
                material /= Alphabet.Length;
            }

            return builder.ToString();
        }
    }
}