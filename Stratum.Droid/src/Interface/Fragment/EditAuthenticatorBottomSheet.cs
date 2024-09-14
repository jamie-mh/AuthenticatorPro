// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Generator;

namespace Stratum.Droid.Interface.Fragment
{
    public class EditAuthenticatorBottomSheet : InputAuthenticatorBottomSheet
    {
        public EditAuthenticatorBottomSheet() : base(Resource.Layout.sheetEditAuthenticator,
            Resource.String.editDetails)
        {
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            var type = (AuthenticatorType) Arguments.GetInt("type");
            var issuer = Arguments.GetString("issuer");
            var username = Arguments.GetString("username");
            var secret = Arguments.GetString("secret");
            var pin = Arguments.GetString("pin");
            var algorithm = (HashAlgorithm) Arguments.GetInt("algorithm");
            var digits = Arguments.GetInt("digits");
            var period = Arguments.GetInt("period");
            var counter = Arguments.GetLong("counter");

            InitialAuthenticator = new Authenticator
            {
                Type = type,
                Issuer = issuer,
                Username = username,
                Secret = secret,
                Pin = pin,
                Algorithm = algorithm,
                Digits = digits,
                Period = period,
                Counter = counter
            };

            base.OnCreate(savedInstanceState);
        }

        protected override bool ShouldShowAdvancedOptions(AuthenticatorType type)
        {
            return true;
        }

        protected override bool ShouldShowAdvancedWarning()
        {
            return true;
        }
    }
}