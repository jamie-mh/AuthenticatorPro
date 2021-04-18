using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Data.Source;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Util;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.List
{
    internal sealed class AuthenticatorListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        private const int MaxProgress = 10000;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> MenuClick;

        public event EventHandler MovementStarted;
        public event EventHandler MovementFinished;

        private readonly ViewMode _viewMode;
        private readonly bool _isDark;
        
        private readonly AuthenticatorSource _authSource;
        private readonly CustomIconSource _customIconSource;

        private readonly SemaphoreSlim _customIconDecodeLock;
        private readonly Dictionary<string, Bitmap> _decodedCustomIcons;

        private readonly float _animationScale;

        public AuthenticatorListAdapter(Context context, AuthenticatorSource authSource, CustomIconSource customIconSource, ViewMode viewMode, bool isDark)
        {
            _authSource = authSource;
            _customIconSource = customIconSource;
            _viewMode = viewMode;
            _isDark = isDark;

            _customIconDecodeLock = new SemaphoreSlim(1, 1);
            _decodedCustomIcons = new Dictionary<string, Bitmap>();

            _animationScale = Settings.Global.GetFloat(context.ContentResolver, Settings.Global.AnimatorDurationScale, 1.0f);
        }

        public override int ItemCount => _authSource.GetView().Count;

        public void MoveItemView(int oldPosition, int newPosition)
        {
            _authSource.Swap(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public async void NotifyMovementFinished()
        {
            MovementFinished?.Invoke(this, null);

            try
            {
                await _authSource.CommitRanking();
            }
            catch
            {
                // Cannot revert, keep going
            }
        }

        public void NotifyMovementStarted()
        {
            MovementStarted?.Invoke(this, null);
        }

        public override long GetItemId(int position)
        {
            return _authSource.Get(position).Secret.GetHashCode();
        }

        public override async void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;

            var holder = (AuthenticatorListHolder) viewHolder;

            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            holder.Username.Visibility = String.IsNullOrEmpty(auth.Username)
                ? ViewStates.Gone
                : ViewStates.Visible;

            holder.Code.Text = CodeUtil.PadCode(auth.GetCode(), auth.Digits);

            if(auth.Icon != null && auth.Icon.StartsWith(CustomIcon.Prefix))
            {
                var id = auth.Icon[1..];
                var customIcon = _customIconSource.Get(id);

                if(customIcon != null)
                    holder.Icon.SetImageBitmap(await DecodeCustomIcon(customIcon)); 
                else
                    holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, _isDark));
            }
            else
                holder.Icon.SetImageResource(IconResolver.GetService(auth.Icon, _isDark));

            switch(auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                    holder.RefreshButton.Visibility = ViewStates.Gone;
                    holder.ProgressBar.Visibility = ViewStates.Visible;
                    UpdateProgressBar(holder.ProgressBar, auth.Period, auth.TimeRenew);
                    break;

                case GenerationMethod.Counter:
                    var isExpired = auth.TimeRenew <= DateTimeOffset.UtcNow;
                    holder.RefreshButton.Visibility = isExpired ? ViewStates.Visible : ViewStates.Invisible;
                    holder.ProgressBar.Visibility = ViewStates.Invisible;
                    break;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position, IList<Object> payloads)
        {
            if(payloads == null || payloads.Count == 0)
            {
                OnBindViewHolder(viewHolder, position);
                return;
            }

            var auth = _authSource.Get(position);
            var holder = (AuthenticatorListHolder) viewHolder;
            
            var isExpired = (bool) payloads[0];
            
            if(isExpired)
                holder.Code.Text = CodeUtil.PadCode(auth.GetCode(), auth.Digits);
            
            switch(auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                    UpdateProgressBar(holder.ProgressBar, auth.Period, auth.TimeRenew);
                    break;
                
                case GenerationMethod.Counter:
                    holder.RefreshButton.Visibility = isExpired ? ViewStates.Visible : ViewStates.Invisible;
                    break;
            } 
        }

        private async Task<Bitmap> DecodeCustomIcon(CustomIcon customIcon)
        {
            if(_decodedCustomIcons.TryGetValue(customIcon.Id, out var bitmap))
                return bitmap;

            await _customIconDecodeLock.WaitAsync();
            
            try
            {
                if(_decodedCustomIcons.TryGetValue(customIcon.Id, out bitmap))
                    return bitmap;

                bitmap = await BitmapFactory.DecodeByteArrayAsync(customIcon.Data, 0, customIcon.Data.Length);
                _decodedCustomIcons.Add(customIcon.Id, bitmap);
                return bitmap;
            }
            finally
            {
                _customIconDecodeLock.Release();
            }
        }

        public void Tick(int startPos, int endPos)
        {
            if(startPos < 0 || endPos < 0)
                return;
            
            endPos = Math.Min(endPos, ItemCount - 1);
            var now = DateTimeOffset.UtcNow;
            
            for(var i = startPos; i <= endPos; ++i)
            {
                var auth = _authSource.Get(i);

                if(auth == null)
                    continue;

                if(auth.TimeRenew <= now)
                    NotifyItemChanged(i, true);
                else if(_animationScale == 0)
                    NotifyItemChanged(i, false);
            }
        }
        
        private void UpdateProgressBar(ProgressBar progressBar, int period, DateTimeOffset timeRenew)
        {
            var millisRemaining = Math.Max(timeRenew.ToUnixTimeMilliseconds() - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 0);
            var periodMillis = period * 1000;
            var progress = (int) Math.Round((double) MaxProgress * millisRemaining / periodMillis);
            
            progressBar.Progress = progress;
            
            if(_animationScale == 0)
                return;
            
            var animator = ObjectAnimator.OfInt(progressBar, "progress", 0);
            var duration = (int) (millisRemaining / _animationScale);
            
            animator.SetDuration(duration);
            animator.SetInterpolator(new LinearInterpolator());
            animator.Start();
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
            holder.ItemView.Click += delegate { ItemClick.Invoke(this, holder.AdapterPosition); };
            holder.MenuButton.Click += delegate { MenuClick.Invoke(this, holder.AdapterPosition); };
            holder.RefreshButton.Click += delegate { OnRefreshClick(holder.AdapterPosition); };

            return holder;
        }

        private async void OnRefreshClick(int position)
        {
            await _authSource.IncrementCounter(position);
            NotifyItemChanged(position);
        }
    }
}