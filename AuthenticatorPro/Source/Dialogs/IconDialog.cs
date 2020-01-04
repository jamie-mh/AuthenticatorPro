using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.AuthenticatorList;
using AuthenticatorPro.IconList;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace AuthenticatorPro.Dialogs
{
    internal class IconDialog : DialogFragment
    {
        private readonly IconSource _iconSource;

        private readonly Action<object, EventArgs> _itemClick;
        private readonly Action<object, EventArgs> _negativeButtonEvent;
        private IconAdapter _iconAdapter;
        private RecyclerView _iconList;

        private EditText _searchText;

        public IconDialog(Action<object, EventArgs> itemClick, Action<object, EventArgs> negative, int position, bool isDark)
        {
            _itemClick = itemClick;
            _negativeButtonEvent = negative;
            _iconSource = new IconSource(isDark);

            Position = position;
        }

        public int Position { get; }
        public string IconKey { get; private set; }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.changeIcon);

            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogIcon, null);
            _searchText = view.FindViewById<EditText>(Resource.Id.dialogIcon_search);
            _iconList = view.FindViewById<RecyclerView>(Resource.Id.dialogIcon_list);
            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            _searchText.TextChanged += SearchChanged;

            _iconAdapter = new IconAdapter(Context, _iconSource);
            _iconAdapter.ItemClick += ItemClick;
            _iconAdapter.HasStableIds = true;

            _iconList.SetAdapter(_iconAdapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);

            var layout = new AuthListGridLayoutManager(Context, 6);
            _iconList.SetLayoutManager(layout);

            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);
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