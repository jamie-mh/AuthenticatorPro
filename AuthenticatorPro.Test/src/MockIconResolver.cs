// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.Test
{
    internal class MockIconResolver : IIconResolver
    {
        public string FindServiceKeyByName(string name)
        {
            return "default";
        }
    }
}