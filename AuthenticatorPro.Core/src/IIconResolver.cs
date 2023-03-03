// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Core
{
    public interface IIconResolver
    {
        public string FindServiceKeyByName(string name);
    }
}