using System;
using System.Linq;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.List;


namespace AuthenticatorPro.Fragment
{
    internal class ChangeIconBottomSheet : BottomSheet
    {
        public event EventHandler<IconSelectedEventArgs> IconSelected;

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

            _iconListAdapter = new IconListAdapter(Context, _iconSource);
            _iconListAdapter.ItemClick += OnItemClick;
            _iconListAdapter.HasStableIds = true;

            _iconList.SetAdapter(_iconListAdapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);

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