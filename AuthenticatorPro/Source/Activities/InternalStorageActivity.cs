using Android;
using Android.Content.PM;
using Android.Runtime;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace AuthenticatorPro.Activities
{
    internal abstract class InternalStorageActivity : LightDarkActivity
    {
        private const int PermissionStorageCode = 0;

        protected bool GetStoragePermission()
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.WriteExternalStorage }, PermissionStorageCode);
                return false;
            }

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionStorageCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    OnStoragePermissionGranted();
                else
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected abstract void OnStoragePermissionGranted();
    }
}