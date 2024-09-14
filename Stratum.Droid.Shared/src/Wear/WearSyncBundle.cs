// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace Stratum.Droid.Shared.Wear
{
    public class WearSyncBundle
    {
        public List<WearAuthenticator> Authenticators { get; set; }
        public List<WearCategory> Categories { get; set; }
        public List<WearCustomIcon> CustomIcons { get; set; }
        public WearPreferences Preferences { get; set; }
    }
}