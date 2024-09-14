// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Stratum.Core;
using Stratum.Core.Generator;
using Stratum.Droid.Shared.Wear;

namespace Stratum.WearOS.Util
{
    public static class AuthenticatorUtil
    {
        public static IGenerator GetGenerator(AuthenticatorType type, string secret, string pin, int period,
            HashAlgorithm algorithm, int digits)
        {
            return type switch
            {
                AuthenticatorType.MobileOtp => new MobileOtp(secret, pin),
                AuthenticatorType.SteamOtp => new SteamOtp(secret),
                AuthenticatorType.YandexOtp => new YandexOtp(secret, pin),
                _ => new Totp(secret, period, algorithm, digits)
            };
        }

        public static IGenerator GetGenerator(WearAuthenticator auth)
        {
            return GetGenerator(auth.Type, auth.Secret, auth.Pin, auth.Period, auth.Algorithm, auth.Digits);
        }

        public static ValueTuple<string, long> GetCodeAndRemainingSeconds(IGenerator generator, int period)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var generationOffset = now - now % period;

            var code = generator.Compute(generationOffset);
            var renewTime = generationOffset + period;

            var secondsRemaining = Math.Max(renewTime - now, 0);

            return new ValueTuple<string, long>(code, secondsRemaining);
        }
    }
}