// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Shared.Data
{
    public interface IIconResolver
    {
        public string FindServiceKeyByName(string name);
    }
}