// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Text;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Views;
using AndroidX.Core.Graphics;
using AndroidX.Core.Widget;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Dialog;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextView;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    public class ErrorActivity : BaseActivity
    {
        private string _exception;

        public ErrorActivity() : base(Resource.Layout.activityError)
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SupportActionBar.SetTitle(Resource.String.error);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

            _exception = Intent.GetStringExtra("exception");
            var textError = FindViewById<MaterialTextView>(Resource.Id.errorText);
            textError.Text = _exception;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.error, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;

                case Resource.Id.actionReport:
                    ShowReportOptionDialog();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ShowReportOptionDialog()
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetTitle(Resource.String.reportVia);

            builder.SetItems(Resource.Array.reportOptions, (_, args) =>
            {
                if (args.Which == 0)
                {
                    ReportGitHub();
                }
                else
                {
                    ReportEmail();
                }
            });

            builder.Create().Show();
        }

        private void ReportGitHub()
        {
            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("error", _exception);
            clipboard.PrimaryClip = clip;

            ShowSnackbar(Resource.String.errorCopiedToClipboard, Snackbar.LengthShort);
            var intent = new Intent(Intent.ActionView, Uri.Parse($"{GetString(Resource.String.githubRepo)}/issues"));

            try
            {
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.webBrowserMissing, Snackbar.LengthShort);
            }
        }

        private string GetAppVersion()
        {
            try
            {
                return PackageUtil.GetVersionName(PackageManager, PackageName);
            }
            catch
            {
                return null;
            }
        }

        private static string GetDeviceName()
        {
            if (Build.Manufacturer != null && Build.Model.StartsWith(Build.Manufacturer))
            {
                return Build.Model;
            }

            return $"{Build.Manufacturer} {Build.Model}";
        }

        private static string GetAndroidVersion()
        {
            return Build.VERSION.Release == null
                ? null
                : $"{Build.VERSION.Release} (API {Build.VERSION.SdkInt})";
        }

        private string DecodeEmail()
        {
            var encoded = GetString(Resource.String.contactEmail).ToCharArray();
            var decoded = new char[encoded.Length];

            for (var i = 0; i < encoded.Length; ++i)
            {
                decoded[i] = i % 2 == 0 ? ++encoded[i] : --encoded[i];
            }

            return new string(decoded);
        }

        private void ReportEmail()
        {
            var intent = new Intent(Intent.ActionSendto);
            intent.SetData(Uri.Parse("mailto:"));
            intent.PutExtra(Intent.ExtraEmail, new[] { DecodeEmail() });
            intent.PutExtra(Intent.ExtraSubject, "Bug report");

            var body = new StringBuilder();
            body.AppendLine("== Please describe the bug here ==");
            body.AppendLine();
            body.AppendLine();
            body.AppendLine();
            body.AppendLine($"App version: {GetAppVersion()}");
            body.AppendLine();
            body.AppendLine($"Device name: {GetDeviceName()}");
            body.AppendLine();
            body.AppendLine($"Android version: {GetAndroidVersion()}");
            body.AppendLine();
            body.AppendLine("Error log:");
            body.AppendLine();
            body.Append(_exception);

            intent.PutExtra(Intent.ExtraText, body.ToString());

            try
            {
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.emailClientMissing, Snackbar.LengthShort);
            }
        }

        protected override void OnApplySystemBarInsets(Insets insets)
        {
            base.OnApplySystemBarInsets(insets);
            var scrollView = FindViewById<NestedScrollView>(Resource.Id.nestedScrollView);
            scrollView.SetPadding(0, 0, 0, insets.Bottom);
        }
    }
}