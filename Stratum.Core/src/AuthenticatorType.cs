// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Stratum.Core.Generator;

namespace Stratum.Core
{
    public enum AuthenticatorType
    {
        Hotp = 1,
        Totp = 2,
        MobileOtp = 3,
        SteamOtp = 4,
        YandexOtp = 5
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

        public static bool HasBase32Secret(this AuthenticatorType type)
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

        public static bool HasVariableAlgorithm(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => true,
                AuthenticatorType.Totp => true,
                AuthenticatorType.MobileOtp => false,
                AuthenticatorType.SteamOtp => false,
                AuthenticatorType.YandexOtp => false,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static bool HasVariablePeriod(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => false,
                AuthenticatorType.Totp => true,
                AuthenticatorType.MobileOtp => false,
                AuthenticatorType.SteamOtp => false,
                AuthenticatorType.YandexOtp => false,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static bool HasPin(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Hotp => false,
                AuthenticatorType.Totp => false,
                AuthenticatorType.MobileOtp => true,
                AuthenticatorType.SteamOtp => false,
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
                AuthenticatorType.MobileOtp => MobileOtp.Digits,
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
                AuthenticatorType.MobileOtp => MobileOtp.Digits,
                AuthenticatorType.SteamOtp => SteamOtp.Digits,
                AuthenticatorType.YandexOtp => YandexOtp.Digits,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static int GetMinPinLength(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Totp => throw new NotSupportedException(),
                AuthenticatorType.Hotp => throw new NotSupportedException(),
                AuthenticatorType.MobileOtp => MobileOtp.PinLength,
                AuthenticatorType.SteamOtp => throw new NotSupportedException(),
                AuthenticatorType.YandexOtp => 4,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        public static int GetMaxPinLength(this AuthenticatorType type)
        {
            return type switch
            {
                AuthenticatorType.Totp => throw new NotSupportedException(),
                AuthenticatorType.Hotp => throw new NotSupportedException(),
                AuthenticatorType.MobileOtp => MobileOtp.PinLength,
                AuthenticatorType.SteamOtp => throw new NotSupportedException(),
                AuthenticatorType.YandexOtp => 16,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }
}