// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.View;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Shared.View
{
    public interface IIconView : IView<KeyValuePair<string, int>>
    {
        public string Search { get; set; }
        public bool UseDarkTheme { get; set; }
    }
}