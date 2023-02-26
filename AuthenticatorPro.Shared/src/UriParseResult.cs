// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;

namespace AuthenticatorPro.Shared
{
    public class UriParseResult
    {
        public Authenticator Authenticator { get; set; }
        public int PinLength { get; set; }
    }
}