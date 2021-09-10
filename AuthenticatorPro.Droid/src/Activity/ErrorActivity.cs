// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using System.Text;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class ErrorActivity : BaseActivity
    {
        private string _exception;

        public ErrorActivity() : base(Resource.Layout.activityError) { }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.error);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _exception = Intent.GetStringExtra("exception");
            var textError = FindViewById<TextView>(Resource.Id.errorText);
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

            Toast.MakeText(this, Resource.String.errorCopiedToClipboard, ToastLength.Short).Show();

            var intent = new Intent(Intent.ActionView, Uri.Parse($"{Constants.GitHubRepo}/issues"));

            try
            {
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                Toast.MakeText(this, Resource.String.webBrowserMissing, ToastLength.Short).Show();
            }
        }

        private string GetAppVersion()
        {
            try
            {
                var packageInfo = PackageManager.GetPackageInfo(PackageName!, 0);
                return packageInfo.VersionName;
            }
            catch
            {
                return null;
            }
        }

        private string GetDeviceName()
        {
            if (Build.Manufacturer != null && Build.Model.StartsWith(Build.Manufacturer))
            {
                return Build.Model;
            }

            return $"{Build.Manufacturer} {Build.Model}";
        }

        private string GetAndroidVersion()
        {
            return Build.VERSION.Release == null
                ? null
                : $"{Build.VERSION.Release} (API {Build.VERSION.SdkInt})";
        }

        private void ReportEmail()
        {
            var intent = new Intent(Intent.ActionSendto);
            intent.SetData(Uri.Parse("mailto:"));
            intent.PutExtra(Intent.ExtraEmail, new[] { Constants.ContactEmail });
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
                Toast.MakeText(this, Resource.String.emailClientMissing, ToastLength.Short).Show();
            }
        }
    }
}