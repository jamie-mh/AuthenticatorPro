// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Util;
using AuthenticatorPro.WearOS.CustomView;
using AuthenticatorPro.WearOS.Util;
using System;
using System.Timers;

namespace AuthenticatorPro.WearOS.Activity
{
    [Activity]
    internal class CodeActivity : AppCompatActivity
    {
        private IGenerator _generator;

        private int _period;
        private int _digits;
        private int _codeGroupSize;

        private AuthProgressLayout _authProgressLayout;
        private TextView _codeTextView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activityCode);

            var preferences = new PreferenceWrapper(this);
            _codeGroupSize = preferences.CodeGroupSize;

            _authProgressLayout = FindViewById<AuthProgressLayout>(Resource.Id.layoutAuthProgress);

            _codeTextView = FindViewById<TextView>(Resource.Id.textCode);

            var issuerText = FindViewById<TextView>(Resource.Id.textIssuer);
            var usernameText = FindViewById<TextView>(Resource.Id.textUsername);

            var username = Intent.Extras.GetString("username");
            var issuer = Intent.Extras.GetString("issuer");

            issuerText.Text = issuer;

            if (String.IsNullOrEmpty(username))
            {
                usernameText.Visibility = ViewStates.Gone;
            }
            else
            {
                usernameText.Text = username;
            }

            var iconView = FindViewById<ImageView>(Resource.Id.imageIcon);
            var hasCustomIcon = Intent.Extras.GetBoolean("hasCustomIcon");

            if (hasCustomIcon)
            {
                var bitmap = (Bitmap) Intent.Extras.GetParcelable("icon");

                if (bitmap != null)
                {
                    iconView.SetImageBitmap(bitmap);
                }
                else
                {
                    iconView.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
                }
            }
            else
            {
                iconView.SetImageResource(IconResolver.GetService(Intent.Extras.GetString("icon"), true));
            }

            _period = Intent.Extras.GetInt("period");
            _digits = Intent.Extras.GetInt("digits");

            var algorithm = (HashAlgorithm) Intent.Extras.GetInt("algorithm");

            var secret = Intent.Extras.GetString("secret");
            var type = (AuthenticatorType) Intent.Extras.GetInt("type");

            _generator = type switch
            {
                AuthenticatorType.MobileOtp => new MobileOtp(secret, _digits),
                AuthenticatorType.SteamOtp => new SteamOtp(secret),
                _ => new Totp(secret, _period, algorithm, _digits)
            };

            _authProgressLayout.Period = _period * 1000;
            _authProgressLayout.TimerFinished += Refresh;
        }

        protected override void OnResume()
        {
            base.OnResume();
            Refresh();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _authProgressLayout.StopTimer();
        }

        private void Refresh(object sender = null, ElapsedEventArgs args = null)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var generationOffset = now - (now % _period);

            var code = _generator.Compute(generationOffset);
            var renewTime = generationOffset + _period;

            var secondsRemaining = Math.Max(renewTime - now, 0);

            RunOnUiThread(delegate
            {
                _codeTextView.Text = CodeUtil.PadCode(code, _digits, _codeGroupSize);
                _authProgressLayout.StartTimer((_period - secondsRemaining) * 1000);
            });
        }
    }
}