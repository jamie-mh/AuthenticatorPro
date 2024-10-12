// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AndroidX.Activity.Result;
using Java.Lang;

namespace Stratum.Droid.Callback
{
    public class ActivityResultCallback : Object, IActivityResultCallback
    {
        public void OnActivityResult(Object obj)
        {
            Result?.Invoke(this, obj);
        }

        public event System.EventHandler<Object> Result;
    }
}