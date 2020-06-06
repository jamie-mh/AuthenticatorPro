using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.List;
using Environment = Android.OS.Environment;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity]
    internal class FileActivity : LightDarkActivity
    {
        public enum Mode
        {
            Save = 0,
            Open = 1
        }

        private EditText _filenameText;
        private FileListAdapter _fileListAdapter;

        private FileSource _fileSource;
        private Mode _mode;
        private Button _saveButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityFile);

            _mode = (Mode) Intent.GetIntExtra("mode", (int) Mode.Save);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityFile_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _fileSource = new FileSource(Environment.ExternalStorageDirectory.AbsolutePath);
            _fileListAdapter = new FileListAdapter(_fileSource);
            _fileListAdapter.SetHasStableIds(true);

            var list = FindViewById<RecyclerView>(Resource.Id.activityFile_list);
            list.SetAdapter(_fileListAdapter);
            list.HasFixedSize = true;
            list.SetItemViewCacheSize(20);

            switch(_mode)
            {
                case Mode.Open:
                    list.SetPadding(0, 0, 0, 0);
                    FindViewById<RelativeLayout>(Resource.Id.activityFile_saveLayout).Visibility = ViewStates.Gone;
                    _fileListAdapter.FileClick += FileClick;
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

            var layout = new AnimatedGridLayoutManager(this, 1);
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

            Intent.PutExtra("path", _fileSource.CurrentPath);
            Intent.PutExtra("filename", _filenameText.Text);

            SetResult(Result.Ok, Intent);
            Finish();
        }

        private void FileClick(object sender, int position)
        {
            Intent.PutExtra("path", _fileSource.CurrentPath);
            Intent.PutExtra("filename", _fileSource.Listing[position].Name);

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
            if(_fileSource.CanNavigateUp)
            {
                _fileSource.Navigate(0);
                _fileListAdapter.NotifyDataSetChanged();
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