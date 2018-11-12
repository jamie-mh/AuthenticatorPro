using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using ProAuth.Utilities;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace ProAuth
{
    internal class DialogIcon : DialogFragment
    {
        public int Position { get; }
        public string IconKey { get; private set; }

        private EditText _searchText;
        private RecyclerView _iconList;
        private readonly IconSource _iconSource;
        private IconAdapter _iconAdapter;

        private readonly Action<object, EventArgs> _itemClick;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        public DialogIcon(Action<object, EventArgs> itemClick, Action<object, EventArgs> negative, int position)
        {
            _itemClick = itemClick;
            _negativeButtonEvent = negative;
            _iconSource = new IconSource();

            Position = position;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.changeIcon);

            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogIcon, null);
            _searchText = view.FindViewById<EditText>(Resource.Id.dialogIcon_search);
            _iconList = view.FindViewById<RecyclerView>(Resource.Id.dialogIcon_list);
            alert.SetView(view);

            AlertDialog dialog = alert.Create();
            dialog.Show();

            _searchText.TextChanged += SearchChanged;

            _iconAdapter = new IconAdapter(Context, _iconSource);
            _iconAdapter.ItemClick += ItemClick;
            _iconAdapter.HasStableIds = true;

            _iconList.SetAdapter(_iconAdapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);

            CustomGridLayoutManager layout = new CustomGridLayoutManager(Context, 8);
            _iconList.SetLayoutManager(layout);

            Button cancelButton = dialog.GetButton((int) DialogButtonType.Negative);
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            _iconSource.SetSearch(e.Text.ToString());
            _iconAdapter.NotifyDataSetChanged();
        }

        private void ItemClick(object sender, int e)
        {
            IconKey = _iconSource.List.ElementAt(e).Key;
            _itemClick.Invoke(sender, null);
        }
    }
}