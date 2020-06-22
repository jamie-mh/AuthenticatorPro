using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.List
{
    internal sealed class AuthenticatorListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        private const int MaxCodeGroupSize = 4;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> MenuClick;

        public event EventHandler MovementStarted;
        public event EventHandler MovementFinished;

        private readonly bool _isCompact;
        private readonly bool _isDark;
        private readonly AuthenticatorSource _source;


        public AuthenticatorListAdapter(AuthenticatorSource source, bool isDark, bool isCompact)
        {
            _isDark = isDark;
            _isCompact = isCompact;
            _source = source;
        }

        public override int ItemCount => _source.Authenticators.Count;

        public async void MoveItem(int oldPosition, int newPosition)
        {
            NotifyItemMoved(oldPosition, newPosition);
            await _source.Move(oldPosition, newPosition);
        }

        public void OnMovementFinished()
        {
            MovementFinished?.Invoke(this, null);
        }

        public void OnMovementStarted()
        {
            MovementStarted?.Invoke(this, null);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = _source.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var holder = (AuthenticatorListHolder) viewHolder;

            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            holder.Username.Visibility = auth.Username == ""
                ? ViewStates.Gone
                : ViewStates.Visible;

            var code = auth.GetCode();

            if(code == null)
                code = "".PadRight(auth.Digits, '-');

            var spacesInserted = 0;
            var groupSize = Math.Min(MaxCodeGroupSize, auth.Digits / 2);

            for(var i = 0; i < auth.Digits; ++i)
            {
                if(i % groupSize == 0 && i > 0)
                {
                    code = code.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            holder.Icon.SetImageResource(Icon.GetService(auth.Icon, _isDark));

            switch(auth.Type)
            {
                case AuthenticatorType.Totp:
                    TotpViewBind(holder, auth);
                    break;

                case AuthenticatorType.Hotp:
                    HotpViewBind(holder, auth);
                    break;
            }

            holder.Code.Text = code;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if(payloads == null || payloads.Count == 0)
                OnBindViewHolder(viewHolder, position);
            else
            {
                var auth = _source.Authenticators[position];
                var holder = (AuthenticatorListHolder) viewHolder;

                switch(auth.Type)
                {
                    case AuthenticatorType.Totp:
                        holder.ProgressBar.Progress = GetRemainingProgress(auth);
                        break;

                    case AuthenticatorType.Hotp:
                        holder.RefreshButton.Visibility = ViewStates.Visible;
                        break;
                }
            }
        }

        private static int GetRemainingProgress(Authenticator auth)
        {
            var secondsRemaining = (auth.TimeRenew - DateTime.Now).TotalSeconds;
            return (int) Math.Ceiling(100d * secondsRemaining / auth.Period);
        }

        private static void TotpViewBind(AuthenticatorListHolder holder, Authenticator auth)
        {
            holder.RefreshButton.Visibility = ViewStates.Gone;
            holder.ProgressBar.Visibility = ViewStates.Visible;
            holder.ProgressBar.Progress = GetRemainingProgress(auth);
        }

        private static void HotpViewBind(AuthenticatorListHolder holder, Authenticator auth)
        {
            holder.RefreshButton.Visibility = auth.TimeRenew < DateTime.Now
                ? ViewStates.Visible
                : ViewStates.Gone;

            holder.ProgressBar.Visibility = ViewStates.Invisible;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layout = _isCompact ? Resource.Layout.listItemAuthCompact: Resource.Layout.listItemAuth;
            var itemView = LayoutInflater.From(parent.Context).Inflate(layout, parent, false);

            var holder = new AuthenticatorListHolder(itemView);
            holder.Click += ItemClick;
            holder.MenuClick += MenuClick;
            holder.RefreshClick += OnRefreshClick;

            return holder;
        }

        private async void OnRefreshClick(object sender, int position)
        {
            await _source.IncrementCounter(position);
            NotifyItemChanged(position);
        }

        public override long GetItemId(int position)
        {
            return _source.Authenticators[position].GetHashCode();
        }
    }
}