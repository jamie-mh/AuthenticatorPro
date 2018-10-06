using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProAuth.Utilities;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using ProAuth.Data;
using Newtonsoft.Json;
using System.IO;
using Environment = Android.OS.Environment;
using PCLCrypto;
using System.Security.Cryptography;
using Android.Hardware.Fingerprints;
using Android.Support.V4.Hardware.Fingerprint;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace ProAuth
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity: AppCompatActivity
    {
        private EditText _textPassword;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityLogin);

            _textPassword = FindViewById<EditText>(Resource.Id.activityExport_password);

            CrossFingerprint.SetCurrentActivityResolver(() => this);
            AuthenticationRequestConfiguration config = new AuthenticationRequestConfiguration("boi");
            config.UseDialog = false;

            var result = await CrossFingerprint.Current.AuthenticateAsync(config);
            if (result.Authenticated)
            {
                Finish();
            }
            else
            {
                Toast.MakeText(this, "failure", ToastLength.Short).Show();
            }
        }

        public override bool OnSupportNavigateUp()
        {
            return false;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return false;
        }

        public override void OnBackPressed()
        {

        }
    }
}