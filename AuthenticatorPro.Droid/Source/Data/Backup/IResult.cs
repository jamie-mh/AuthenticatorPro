// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;

namespace AuthenticatorPro.Droid.Data.Backup
{
    internal interface IResult
    {
        public bool IsVoid();
        public string ToString(Context context);
    }
}