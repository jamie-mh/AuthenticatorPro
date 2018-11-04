using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Java.Lang;
using OtpSharp;
using PlusAuth.Data;

namespace PlusAuth.Utilities
{
    internal sealed class AuthAdapter : RecyclerView.Adapter
    {
        private readonly AuthSource _source;
        private readonly Context _context;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemOptionsClick;

        public AuthAdapter(Context context, AuthSource authSource)
        {
            _source = authSource;
            _context = context;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            Authenticator auth = _source.Get(position);
            AuthHolder holder = (AuthHolder) viewHolder;

            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            holder.Username.Visibility = auth.Username == "" 
                ? ViewStates.Gone 
                : ViewStates.Visible;

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

            Drawable icon = ContextCompat.GetDrawable(_context, Icon.Get(auth.Icon));
            holder.Icon.SetImageDrawable(icon);

            if(auth.Type == OtpType.Totp)
                TotpViewBind(holder, auth);

            else if(auth.Type == OtpType.Hotp) 
                HotpViewBind(holder, auth);

            holder.Code.Text = codePadded;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if(payloads == null || payloads.Count == 0)
            {
                OnBindViewHolder(viewHolder, position);
            }
            else
            {
                AuthHolder holder = (AuthHolder) viewHolder;
                holder.ProgressBar.Progress = (int) payloads[0];
            }
        }

        private static void TotpViewBind(AuthHolder holder, Authenticator auth)
        {
            holder.RefreshButton.Visibility = ViewStates.Gone;
            holder.ProgressBar.Visibility = ViewStates.Visible;
            holder.Counter.Visibility = ViewStates.Invisible;

            //int secondsRemaining = (auth.TimeRenew - DateTime.Now).Seconds;
            //holder.ProgressBar.Progress = 100 * secondsRemaining / auth.Period;
        }

        private static void HotpViewBind(AuthHolder holder, Authenticator auth)
        {
            holder.RefreshButton.Visibility = (auth.TimeRenew < DateTime.Now)
                ? ViewStates.Visible
                : ViewStates.Gone;

            holder.ProgressBar.Visibility = ViewStates.Invisible;
            holder.Counter.Visibility = ViewStates.Visible;
            holder.Counter.Text = auth.Counter.ToString();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.authListItem, parent, false);

            AuthHolder holder = new AuthHolder(itemView, OnItemClick, OnItemOptionsClick, OnRefreshClick);

            return holder;
        }

        public override int ItemCount => _source.Count();

        private void OnItemClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        private void OnItemOptionsClick(int position)
        {
            ItemOptionsClick?.Invoke(this, position);
        }

        private void OnRefreshClick(int position)
        {
            _source.IncrementHotp(position);
            NotifyItemChanged(position);
        }
    }
}