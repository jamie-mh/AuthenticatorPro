using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android;
using Android.Content;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Permission = Android.Content.PM.Permission;
using Android.Runtime;
using Android.Support.V7.Widget;
using ProAuth.Utilities;

namespace ProAuth
{
    [Activity(Label = "FileSaveActivity")]
    public class ActivityFileSave: AppCompatActivity
    {
        private const int PermissionStorageCode = 0;

        private EditText _filenameText;
        private Button _saveButton;

        private FilesystemSource _filesystemSource;
        private FilesystemAdapter _filesystemAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityFileSave);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityFileSave_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.export);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _filenameText = FindViewById<EditText>(Resource.Id.activityFileSave_filename);
            _saveButton = FindViewById<Button>(Resource.Id.activityFileSave_save);
            _saveButton.Click += SaveClick;

            _filesystemSource = new FilesystemSource(Environment.ExternalStorageDirectory.AbsolutePath);
            _filesystemAdapter = new FilesystemAdapter(_filesystemSource);

            RecyclerView list = FindViewById<RecyclerView>(Resource.Id.activityFileSave_list);
            list.SetAdapter(_filesystemAdapter);
            list.HasFixedSize = true;
            list.SetItemViewCacheSize(20);
            list.DrawingCacheEnabled = true;
            list.DrawingCacheQuality = DrawingCacheQuality.High;

            LinearLayoutManager layout = new LinearLayoutManager(this);
            DividerItemDecoration decoration = new DividerItemDecoration(this, layout.Orientation);
            list.AddItemDecoration(decoration);
            list.SetLayoutManager(layout);
        }

        private void SaveClick(object sender, System.EventArgs e)
        {
            Intent.PutExtra("path", _filesystemSource.CurrentPath);
            Intent.PutExtra("filename", _filenameText.Text);

            SetResult(Result.Ok, Intent);
            Finish();
        }

        private bool GetStoragePermission()
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage)
               != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, 
                    new[] { Manifest.Permission.WriteExternalStorage }, PermissionStorageCode);
                return false;
            }

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionStorageCode)
            {
                if(grantResults.Length <= 0 || grantResults[0] != Permission.Granted)
                {
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
                    Finish();
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override bool OnSupportNavigateUp()
        {
            NavigateUp();
            return false;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                Cancel();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            NavigateUp();
        }

        private void NavigateUp()
        {
            if(_filesystemSource.CanNavigateUp)
            {
                _filesystemSource.Navigate(0);
                _filesystemAdapter.NotifyDataSetChanged();
                return;
            }

            Cancel();
        }

        private void Cancel()
        {
            SetResult(Result.Canceled, Intent);
            Finish();
        }
    }
}