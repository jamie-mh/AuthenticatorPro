// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearAuthenticator
    {
        public readonly AuthenticatorType Type;
        public readonly string Secret;
        public readonly string Icon;
        public readonly string Issuer;
        public readonly string Username;
        public readonly int Period;
        public readonly int Digits;
        public readonly HashAlgorithm Algorithm;
        public readonly int Ranking;
        public readonly List<WearAuthenticatorCategory> Categories;

        public WearAuthenticator(AuthenticatorType type, string secret, string icon, string issuer, string username,
            int period, int digits, HashAlgorithm algorithm, int ranking, List<WearAuthenticatorCategory> categories)
        {
            Type = type;
            Secret = secret;
            Icon = icon;
            Issuer = issuer;
            Username = username;
            Period = period;
            Digits = digits;
            Algorithm = algorithm;
            Ranking = ranking;
            Categories = categories;
        }
    }
}