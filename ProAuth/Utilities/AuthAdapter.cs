using System;
using Android.Support.V7.Widget;
using Android.Views;
using OtpSharp;
using ProAuth.Data;

namespace ProAuth.Utilities
{
    internal sealed class AuthAdapter : RecyclerView.Adapter
    {
        private readonly AuthSource _authSource;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemOptionsClick;

        public AuthAdapter(AuthSource authSource)
        {
            _authSource = authSource;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            Authenticator auth = _authSource.GetNth(position);
            AuthHolder authHolder = (AuthHolder) holder;

            authHolder.Issuer.Text = auth.Issuer;
            authHolder.Username.Text = auth.Username;
            authHolder.Username.Visibility = (auth.Username == "") ? ViewStates.Gone : ViewStates.Visible;

            string codePadded = auth.Code;
            int spacesInserted = 0;
            int length = codePadded.Length;

            for(int i = 0; i < length; ++i)
            {
                if(i % 3 == 0 && i > 0)
                {
                    codePadded = codePadded.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            if(auth.Type == OtpType.Hotp)
            {
                authHolder.RefreshButton.Visibility = ViewStates.Visible;
                authHolder.Timer.Visibility = ViewStates.Invisible;
                authHolder.Counter.Visibility = ViewStates.Visible;

                authHolder.Counter.Text = $@"Counter: {auth.Counter.ToString()}";
            }
            else if(auth.Type == OtpType.Totp)
            {
                authHolder.Timer.Text = (auth.TimeRenew - DateTime.Now).Seconds.ToString();
                authHolder.Counter.Visibility = ViewStates.Invisible;
            }

            authHolder.Code.Text = codePadded;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.authListItem, parent, false);

            AuthHolder holder = new AuthHolder(itemView, OnItemClick, OnItemOptionsClick, OnRefreshClick);

            return holder;
        }

        public override int ItemCount => _authSource.Count();

        private void OnItemClick(int e)
        {
            ItemClick?.Invoke(this, e);
        }

        private void OnItemOptionsClick(int e)
        {
            ItemOptionsClick?.Invoke(this, e);
        }

        private void OnRefreshClick(int e)
        {
            _authSource.IncrementCounter(e);
            _authSource.ClearCache(e);
            NotifyItemChanged(e);
        }
    }
}