// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Droid.Data;

namespace AuthenticatorPro.Droid.Activity
{
    internal abstract class SensitiveSubActivity : BaseActivity
    {
        protected SensitiveSubActivity(int layout) : base(layout) { }
        
        protected override void OnResume()
        {
            base.OnResume();
            
            if(!Database.IsOpen)
                Finish();
        }
    }
}