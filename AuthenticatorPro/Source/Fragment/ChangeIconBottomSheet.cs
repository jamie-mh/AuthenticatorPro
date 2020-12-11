using System;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.List;
using Google.Android.Material.Button;


namespace AuthenticatorPro.Fragment
{
    internal class ChangeIconBottomSheet : BottomSheet
    {
        public event EventHandler<IconSelectedEventArgs> IconSelect;
        public event EventHandler UseCustomIconClick;

        private readonly IconSource _iconSource;
        private readonly int _itemPosition;

        private IconListAdapter _iconListAdapter;
        private RecyclerView _iconList;
        private EditText _searchText;


        public ChangeIconBottomSheet(int itemPosition, bool isDark)
        {
            RetainInstance = true;

            _itemPosition = itemPosition;
            _iconSource = new IconSource(isDark);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetChangeIcon, null);
            SetupToolbar(view, Resource.String.changeIcon, true);
            
            _searchText = view.FindViewById<EditText>(Resource.Id.editSearch);
            _iconList = view.FindViewById<RecyclerView>(Resource.Id.list);

            _searchText.TextChanged += OnSearchChanged;

            var customIconButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUseCustomIcon);
            customIconButton.Click += (s, e) =>
            {
                UseCustomIconClick?.Invoke(s, e);
                Dismiss();
            };

            _iconListAdapter = new IconListAdapter(Context, _iconSource);
            _iconListAdapter.ItemClick += OnItemClick;
            _iconListAdapter.HasStableIds = true;

            _iconList.SetAdapter(_iconListAdapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);
            _iconList.SetItemAnimator(null);

            var layout = new AutoGridLayoutManager(Context, 140);
            _iconList.SetLayoutManager(layout);

            return view;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            _iconSource.SetSearch(e.Text.ToString());
            _iconListAdapter.NotifyDataSetChanged();
        }

        private void OnItemClick(object sender, int iconPosition)
        {
            var eventArgs = new IconSelectedEventArgs(_itemPosition, _iconSource.Get(iconPosition).Key);
            IconSelect?.Invoke(this, eventArgs);
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