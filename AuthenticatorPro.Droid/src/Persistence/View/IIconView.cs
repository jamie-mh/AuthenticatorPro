// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Persistence.View
{
    public interface IIconView : IView<KeyValuePair<string, int>>
    {
        public string Search { get; set; }
        public bool UseDarkTheme { get; set; }
    }
}