// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Stratum.Core;
using Stratum.Core.Generator;

namespace Stratum.Droid.Shared.Wear
{
    public class WearAuthenticator
    {
        public AuthenticatorType Type { get; set; }
        public string Secret { get; set; }
        public string Pin { get; set; }
        public string Icon { get; set; }
        public string Issuer { get; set; }
        public string Username { get; set; }
        public int Period { get; set; }
        public int Digits { get; set; }
        public HashAlgorithm Algorithm { get; set; }
        public int Ranking { get; set; }
        public int CopyCount { get; set; }
        public List<WearAuthenticatorCategory> Categories { get; set; }
    }
}