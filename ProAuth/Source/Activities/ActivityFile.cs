using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using ProAuth.Utilities;
using ProAuth.Utilities.FilesystemList;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace ProAuth.Activities
{
    [Activity(Label = "FileSaveActivity")]
    public class ActivityFile: AppCompatActivity
    {
        public enum Mode
        {
            Save = 0,
            Open = 1
        };

        private EditText _filenameText;
        private Button _saveButton;
        private Mode _mode;

        private FilesystemSource _filesystemSource;
        private FilesystemAdapter _filesystemAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityFile);

            _mode = (Mode) Intent.GetIntExtra("mode", (int) Mode.Save);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityFile_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back"));

            _filesystemSource = new FilesystemSource(Environment.ExternalStorageDirectory.AbsolutePath);
            _filesystemAdapter = new FilesystemAdapter(_filesystemSource) {
                HasStableIds = true
            };

            RecyclerView list = FindViewById<RecyclerView>(Resource.Id.activityFile_list);
            list.SetAdapter(_filesystemAdapter);
            list.HasFixedSize = true;
            list.SetItemViewCacheSize(20);
            list.DrawingCacheEnabled = true;
            list.DrawingCacheQuality = DrawingCacheQuality.High;

            switch(_mode)
            {
                case Mode.Open:
                    list.SetPadding(0, 0, 0, 0);
                    FindViewById<RelativeLayout>(Resource.Id.activityFile_saveLayout).Visibility = ViewStates.Gone;
                    _filesystemAdapter.BackupClick += BackupClick;
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

            CustomGridLayoutManager layout = new CustomGridLayoutManager(this, 1);
            list.SetLayoutManager(layout);

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

        private void BackupClick(object sender, int position)
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