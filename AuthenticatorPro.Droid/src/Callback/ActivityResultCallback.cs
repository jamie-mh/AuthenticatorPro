// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AndroidX.Activity.Result;
using System;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.Callback
{
    internal class ActivityResultCallback : Object, IActivityResultCallback
    {
        public event EventHandler<ActivityResult> Result;

        public void OnActivityResult(Object obj)
        {
            var result = (ActivityResult) obj;
            Result?.Invoke(this, result);
        }
    }
}