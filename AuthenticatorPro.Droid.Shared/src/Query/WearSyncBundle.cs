// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearSyncBundle
    {
        public readonly List<WearAuthenticator> Authenticators;
        public readonly List<WearCategory> Categories;
        public readonly List<string> CustomIconIds;
        public readonly WearPreferences Preferences;

        public WearSyncBundle(List<WearAuthenticator> authenticators, List<WearCategory> categories,
            List<string> customIconIds, WearPreferences preferences)
        {
            Authenticators = authenticators;
            Categories = categories;
            CustomIconIds = customIconIds;
            Preferences = preferences;
        }
    }
}