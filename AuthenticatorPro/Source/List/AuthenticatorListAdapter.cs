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

        private readonly ViewMode _viewMode;
        private readonly bool _isDark;
        
        private readonly AuthenticatorSource _authenticatorSource;
        private readonly CustomIconSource _customIconSource;

        public enum ViewMode
        {
            Default = 0, Compact = 1, Tile = 2
        }

        public AuthenticatorListAdapter(AuthenticatorSource authenticatorSource, CustomIconSource customIconSource, ViewMode viewMode, bool isDark)
        {
            _authenticatorSource = authenticatorSource;
            _customIconSource = customIconSource;
            _viewMode = viewMode;
            _isDark = isDark;
        }

        public override int ItemCount => _authenticatorSource.Authenticators.Count;

        public async void MoveItem(int oldPosition, int newPosition)
        {
            NotifyItemMoved(oldPosition, newPosition);
            await _authenticatorSource.Move(oldPosition, newPosition);
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
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var holder = (AuthenticatorListHolder) viewHolder;

            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            holder.Username.Visibility = auth.Username == ""
                ? ViewStates.Gone
                : ViewStates.Visible;

            holder.Code.Text = PadCode(auth.GetCode(), auth.Digits);

            if(auth.Icon.StartsWith(CustomIcon.Prefix))
            {
                var id = auth.Icon.Substring(1);
                var customIcon = _customIconSource.Get(id);
                
                if(customIcon != null)
                    holder.Icon.SetImageBitmap(customIcon.GetBitmap()); 
                else
                    holder.Icon.SetImageResource(Icon.GetService(Icon.Default, _isDark));
            }
            else
                holder.Icon.SetImageResource(Icon.GetService(auth.Icon, _isDark));
                
            switch(auth.Type)
            {
                case AuthenticatorType.Totp:
                    holder.RefreshButton.Visibility = ViewStates.Gone;
                    holder.ProgressBar.Visibility = ViewStates.Visible;
                    holder.ProgressBar.Progress = GetRemainingProgress(auth);
                    break;

                case AuthenticatorType.Hotp:
                    holder.RefreshButton.Visibility = auth.TimeRenew < DateTime.Now
                        ? ViewStates.Visible
                        : ViewStates.Gone;

                    holder.ProgressBar.Visibility = ViewStates.Invisible;
                    break;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position, IList<Java.Lang.Object> payloads)
        {
            if(payloads == null || payloads.Count == 0)
            {
                OnBindViewHolder(viewHolder, position);
                return;
            }

            var auth = _authenticatorSource.Authenticators[position];
            var holder = (AuthenticatorListHolder) viewHolder;

            switch(auth.Type)
            {
                case AuthenticatorType.Totp:
                    if(auth.TimeRenew < DateTime.Now)
                        holder.Code.Text = PadCode(auth.GetCode(), auth.Digits);

                    holder.ProgressBar.Progress = GetRemainingProgress(auth);
                    break;

                case AuthenticatorType.Hotp:
                    if(auth.TimeRenew < DateTime.Now)
                        holder.RefreshButton.Visibility = ViewStates.Visible;

                    break;
            }
        }

        private static string PadCode(string code, int digits)
        {
            code ??= "".PadRight(digits, '-');

            var spacesInserted = 0;
            var groupSize = Math.Min(MaxCodeGroupSize, digits / 2);

            for(var i = 0; i < digits; ++i)
            {
                if(i % groupSize == 0 && i > 0)
                {
                    code = code.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            return code;
        }

        private static int GetRemainingProgress(Authenticator auth)
        {
            var secondsRemaining = (auth.TimeRenew - DateTime.Now).TotalSeconds;
            return (int) Math.Floor(100d * secondsRemaining / auth.Period);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layout = _viewMode switch
            {
                ViewMode.Compact => Resource.Layout.listItemAuthCompact,
                ViewMode.Tile => Resource.Layout.listItemAuthTile,
                _ => Resource.Layout.listItemAuth
            };
            
            var itemView = LayoutInflater.From(parent.Context).Inflate(layout, parent, false);

            var holder = new AuthenticatorListHolder(itemView);
            holder.Click += ItemClick;
            holder.MenuClick += MenuClick;
            holder.RefreshClick += OnRefreshClick;

            return holder;
        }

        private async void OnRefreshClick(object sender, int position)
        {
            await _authenticatorSource.IncrementCounter(position);
            NotifyItemChanged(position);
        }

        public override long GetItemId(int position)
        {
            return _authenticatorSource.Authenticators[position].GetHashCode();
        }
    }
}