// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using AuthenticatorPro.Core.Generator;
using SimpleBase;

namespace AuthenticatorPro.Core.Util
{
    public static class SecretUtil
    {
        public static string Clean(string input, AuthenticatorType type)
        {
            if (type.HasBase32Secret())
            {
                input = input.ToUpper();
            }

            input = input.Replace(" ", "");
            input = input.Replace("-", "");

            return input;
        }

        public static void Validate(string secret, AuthenticatorType type)
        {
            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException("Secret cannot be null or empty");
            }

            if (type.HasBase32Secret())
            {
                byte[] bytes;

                try
                {
                    bytes = Base32.Rfc4648.Decode(secret);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Error decoding secret", e);
                }

                if (bytes.Length == 0)
                {
                    throw new ArgumentException("Error decoding secret, output length 0");
                }

                if (type == AuthenticatorType.YandexOtp && bytes.Length < YandexOtp.SecretByteCount)
                {
                    throw new ArgumentException("Secret is too short for Yandex OTP");
                }
            }

            if (type == AuthenticatorType.MobileOtp && secret.Length < MobileOtp.SecretMinLength)
            {
                throw new ArgumentException("Too few characters in secret for mOTP");
            }
        }
    }
}