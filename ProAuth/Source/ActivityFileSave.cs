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

            SupportActionBar.SetTitle(Resource.String.saveFile);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back"));

            _filenameText = FindViewById<EditText>(Resource.Id.activityFileSave_filename);
            _filenameText.Text = Intent.GetStringExtra("filename");

            _saveButton = FindViewById<Button>(Resource.Id.activityFileSave_save);
            _saveButton.Click += SaveClick;

            _filesystemSource = new FilesystemSource(Environment.ExternalStorageDirectory.AbsolutePath);
            _filesystemAdapter = new FilesystemAdapter(_filesystemSource) {
                HasStableIds = true
            };

            RecyclerView list = FindViewById<RecyclerView>(Resource.Id.activityFileSave_list);
            list.SetAdapter(_filesystemAdapter);
            list.HasFixedSize = true;
            list.SetItemViewCacheSize(20);
            list.DrawingCacheEnabled = true;
            list.DrawingCacheQuality = DrawingCacheQuality.High;

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