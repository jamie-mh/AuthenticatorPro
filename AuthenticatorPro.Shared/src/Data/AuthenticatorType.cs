// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Generator;
using System;

namespace AuthenticatorPro.Shared.Data
{
    public enum AuthenticatorType
    {
        Hotp = 1, Totp = 2, MobileOtp = 3, SteamOtp = 4, YandexOtp = 5
    }

    public static class AuthenticatorTypeSpecification
    {
        public static GenerationMethod GetGenerationMethod(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => GenerationMethod.Counter,
                AuthenticatorType.Totp => GenerationMethod.Time,
                AuthenticatorType.MobileOtp => GenerationMethod.Time,
                AuthenticatorType.SteamOtp => GenerationMethod.Time,
                AuthenticatorType.YandexOtp => GenerationMethod.Time,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static bool IsHmacBased(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => true,
                AuthenticatorType.Totp => true,
                AuthenticatorType.MobileOtp => false,
                AuthenticatorType.SteamOtp => true,
                AuthenticatorType.YandexOtp => true,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static int GetDefaultPeriod(this AuthenticatorType type)
        {
            return 30;
        }

        public static int GetDefaultDigits(this AuthenticatorType type)
        {
            return GetMinDigits(type);
        }

        public static int GetMinDigits(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => 6,
                AuthenticatorType.Totp => 6,
                AuthenticatorType.MobileOtp => 6,
                AuthenticatorType.SteamOtp => SteamOtp.Digits,
                AuthenticatorType.YandexOtp => YandexOtp.Digits,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static int GetMaxDigits(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => 8,
                AuthenticatorType.Totp => 10,
                AuthenticatorType.MobileOtp => 10,
                AuthenticatorType.SteamOtp => SteamOtp.Digits,
                AuthenticatorType.YandexOtp => YandexOtp.Digits,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}