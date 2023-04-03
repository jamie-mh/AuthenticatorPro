// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace AuthenticatorPro.Droid.Shared.Wear
{
    public class WearAuthenticatorCategory
    {
        public readonly string CategoryId;
        public readonly int Ranking;

        public WearAuthenticatorCategory(string categoryId, int ranking)
        {
            CategoryId = categoryId;
            Ranking = ranking;
        }
    }
}