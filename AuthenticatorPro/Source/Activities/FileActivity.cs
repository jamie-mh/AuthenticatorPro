using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro;
using AuthenticatorPro.FilesystemList;
using Environment = Android.OS.Environment;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity(Label = "FileSaveActivity")]
    public class FileActivity : AppCompatActivity
    {
        public enum Mode
        {
            Save = 0,
            Open = 1
        }

        private EditText _filenameText;
        private FilesystemAdapter _filesystemAdapter;

        private FilesystemSource _filesystemSource;
        private Mode _mode;
        private Button _saveButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            AuthenticatorPro.Theme.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityFile);

            _mode = (Mode) Intent.GetIntExtra("mode", (int) Mode.Save);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityFile_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back"));

            _filesystemSource = new FilesystemSource(Environment.ExternalStorageDirectory.AbsolutePath);
            _filesystemAdapter = new FilesystemAdapter(_filesystemSource) {
                HasStableIds = true
            };

            var list = FindViewById<RecyclerView>(Resource.Id.activityFile_list);
            list.SetAdapter(_filesystemAdapter);
            list.HasFixedSize = true;
            list.SetItemViewCacheSize(20);

            switch(_mode)
            {
                case Mode.Open:
                    list.SetPadding(0, 0, 0, 0);
                    FindViewById<RelativeLayout>(Resource.Id.activityFile_saveLayout).Visibility = ViewStates.Gone;
                    _filesystemAdapter.FileClick += FileClick;
                    SupportActionBar.SetTitle(Resource.String.openFile);
                    break;

                case Mode.Save:
                    _filenameText = FindViewById<EditText>(Resource.Id.activityFile_filename);
                    _filenameText.Text = Intent.GetStringExtra("filename");

                    _saveButton = FindViewById<Button>(Resource.Id.activityFile_save);
                    _saveButton.Click += SaveClick;
                    SupportActionBar.SetTitle(Resource.String.saveFile);
                    break;
            }

            var layout = new CustomGridLayoutManager(this, 1);
            list.SetLayoutManager(layout);

            var decoration = new DividerItemDecoration(this, layout.Orientation);
            list.AddItemDecoration(decoration);
            list.SetLayoutManager(layout);
        }

        private void SaveClick(object sender, EventArgs e)
        {
            if(_filenameText.Text == "")
            {
                Toast.MakeText(this, Resource.String.noFileName, ToastLength.Short).Show();
                return;
            }

            Intent.PutExtra("path", _filesystemSource.CurrentPath);
            Intent.PutExtra("filename", _filenameText.Text);

            SetResult(Result.Ok, Intent);
            Finish();
        }

        private void FileClick(object sender, int position)
        {
            Intent.PutExtra("path", _filesystemSource.CurrentPath);
            Intent.PutExtra("filename", _filesystemSource.Listing[position].Name);

            SetResult(Result.Ok, Intent);
            Finish();
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