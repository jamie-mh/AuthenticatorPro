using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Javax.Sql;
using PlusAuth.Data;
using PlusAuth.Utilities;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace PlusAuth
{
    internal class IconDialog : DialogFragment
    {
        public int Position { get; }

        private EditText _searchText;
        private RecyclerView _iconList;

        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        public IconDialog(Action<object, EventArgs> positive, Action<object, EventArgs> negative, int position)
        {
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
            Position = position;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.changeIcon);

            alert.SetPositiveButton(Resource.String.ok, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogIcon, null);
            _searchText = view.FindViewById<EditText>(Resource.Id.dialogIcon_search);
            _iconList = view.FindViewById<RecyclerView>(Resource.Id.dialogIcon_list);
            alert.SetView(view);

            AlertDialog dialog = alert.Create();
            dialog.Show();

            IconAdapter adapter = new IconAdapter(Context);
            adapter.ItemClick += ItemClick;

            _iconList.SetAdapter(adapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);
            _iconList.DrawingCacheEnabled = true;
            _iconList.DrawingCacheQuality = DrawingCacheQuality.High;
            _iconList.SetLayoutManager(new GridLayoutManager(Context, 8));

            Button okButton = dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            okButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }

        private void ItemClick(object sender, int e)
        {

        }
    }
}