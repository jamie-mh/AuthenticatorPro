using System;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.List;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;


namespace AuthenticatorPro.Dialog
{
    internal class IconDialog : DialogFragment
    {
        public event EventHandler<IconSelectedEventArgs> IconSelected;

        private readonly IconSource _iconSource;
        private readonly int _itemPosition;

        private IconListAdapter _iconListAdapter;
        private RecyclerView _iconList;
        private EditText _searchText;


        public IconDialog(int itemPosition, bool isDark)
        {
            RetainInstance = true;

            _itemPosition = itemPosition;
            _iconSource = new IconSource(isDark);
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
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

            _searchText.TextChanged += OnSearchChanged;

            _iconListAdapter = new IconListAdapter(Context, _iconSource);
            _iconListAdapter.ItemClick += OnItemClick;
            _iconListAdapter.SetHasStableIds(true);

            _iconList.SetAdapter(_iconListAdapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);

            var layout = new AnimatedGridLayoutManager(Context, 6);
            _iconList.SetLayoutManager(layout);

            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            cancelButton.Click += (sender, e) =>
            {
                dialog.Dismiss();
            };

            return dialog;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            _iconSource.SetSearch(e.Text.ToString());
            _iconListAdapter.NotifyDataSetChanged();
        }

        private void OnItemClick(object sender, int iconPosition)
        {
            var eventArgs = new IconSelectedEventArgs(_itemPosition, _iconSource.List.ElementAt(iconPosition).Key);
            IconSelected?.Invoke(this, eventArgs);
        }

        public class IconSelectedEventArgs : EventArgs
        {
            public readonly int ItemPosition; 
            public readonly string Icon;

            public IconSelectedEventArgs(int itemPosition, string icon)
            {
                ItemPosition = itemPosition;
                Icon = icon;
            }
        }
    }
}