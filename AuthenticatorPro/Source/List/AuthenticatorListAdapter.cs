using System;
using System.Collections.Generic;
using System.Linq;
using Android.Animation;
using Android.Content;
using Android.Provider;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Util;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.List
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

        private readonly float _animationScale;
       
        // Cache the remaining seconds per period, a relative DateTime calculation can be expensive
        // Cache the remaining progress per period, to keep all progressbars in sync
        private readonly Dictionary<int, int> _secondsRemainingPerPeriod;
        private readonly Dictionary<int, int> _progressPerPeriod;
        private readonly Dictionary<int, int> _counterCooldownSeconds;

        public enum ViewMode
        {
            Default = 0, Compact = 1, Tile = 2
        }

        public AuthenticatorListAdapter(Context context, AuthenticatorSource authSource, CustomIconSource customIconSource, ViewMode viewMode, bool isDark)
        {
            _authSource = authSource;
            _customIconSource = customIconSource;
            _viewMode = viewMode;
            _isDark = isDark;

            _secondsRemainingPerPeriod = new Dictionary<int, int>();
            _progressPerPeriod = new Dictionary<int, int>();
            _counterCooldownSeconds = new Dictionary<int, int>();

            _animationScale = Settings.Global.GetFloat(context.ContentResolver, Settings.Global.AnimatorDurationScale, 1.0f);
        }

        public override int ItemCount => _authSource.GetView().Count;

        public void MoveItemView(int oldPosition, int newPosition)
        {
            _authSource.Swap(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public async void NotifyMovementFinished(int oldPosition, int newPosition)
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
                var id = auth.Icon.Substring(1);
                var customIcon = _customIconSource.Get(id);
                
                if(customIcon != null)
                    holder.Icon.SetImageBitmap(await customIcon.GetBitmap()); 
                else
                    holder.Icon.SetImageResource(Icon.GetService(Icon.Default, _isDark));
            }
            else
                holder.Icon.SetImageResource(Icon.GetService(auth.Icon, _isDark));
                
            switch(auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                    holder.RefreshButton.Visibility = ViewStates.Gone;
                    holder.ProgressBar.Visibility = ViewStates.Visible;
                    AnimateProgressBar(holder.ProgressBar, auth.Period);
                    break;

                case GenerationMethod.Counter:
                    holder.RefreshButton.Visibility = auth.TimeRenew < DateTime.UtcNow
                        ? ViewStates.Visible
                        : ViewStates.Gone;

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
            holder.Code.Text = CodeUtil.PadCode(auth.GetCode(), auth.Digits);

            switch(auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                    AnimateProgressBar(holder.ProgressBar, auth.Period);
                    break;
                
                case GenerationMethod.Counter:
                    holder.RefreshButton.Visibility = ViewStates.Visible;
                    _counterCooldownSeconds[auth.Secret.GetHashCode()] = Hotp.CooldownSeconds;
                    break;
            } 
        }

        public void Tick(bool invalidateCache = false)
        {
            if(invalidateCache)
            {
                _secondsRemainingPerPeriod.Clear();
                _progressPerPeriod.Clear();
            }
            
            foreach(var period in _secondsRemainingPerPeriod.Keys.ToList())
                _secondsRemainingPerPeriod[period]--;

            foreach(var key in _counterCooldownSeconds.Keys.ToList())
                _counterCooldownSeconds[key]--;
            
            for(var i = 0; i < _authSource.GetView().Count; ++i)
            {
                var auth = _authSource.Get(i);

                if(auth.Type.GetGenerationMethod() == GenerationMethod.Time &&
                   _secondsRemainingPerPeriod.GetValueOrDefault(auth.Period, -1) <= 0 ||
                   auth.Type.GetGenerationMethod() == GenerationMethod.Counter &&
                   _counterCooldownSeconds.GetValueOrDefault(auth.Secret.GetHashCode(), -1) <= 0)
                    NotifyItemChanged(i, true);
            }

            // Force recalculation of remaining seconds in case of timer drift
            foreach(var period in _secondsRemainingPerPeriod.Keys.ToList().Where(period => _secondsRemainingPerPeriod[period] <= 0))
                _secondsRemainingPerPeriod.Remove(period);

            _progressPerPeriod.Clear();
        }

        private int GetProgress(int period, int secondsRemaining)
        {
            var progress = _progressPerPeriod.GetValueOrDefault(period, -1);

            if(progress > -1)
                return progress;

            progress = (int) Math.Floor((double) MaxProgress * secondsRemaining / period);
            _progressPerPeriod.Add(period, progress);
            return progress;
        }

        private int GetRemainingSeconds(int period)
        {
            var secondsRemaining = _secondsRemainingPerPeriod.GetValueOrDefault(period, -1);

            if(secondsRemaining > -1)
                return secondsRemaining;

            secondsRemaining = period - (int) DateTimeOffset.Now.ToUnixTimeSeconds() % period;
            _secondsRemainingPerPeriod.Add(period, secondsRemaining);
            return secondsRemaining;
        }

        private void AnimateProgressBar(ProgressBar progressBar, int period)
        {
            var secondsRemaining = GetRemainingSeconds(period);
            var progress = GetProgress(period, secondsRemaining);
            progressBar.Progress = progress;
            
            var animator = ObjectAnimator.OfInt(progressBar, "progress", 0);
            var duration = secondsRemaining * 1000;

            if(_animationScale > 0)
                duration = (int) (duration / _animationScale);
            
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
            holder.Click += ItemClick;
            holder.MenuClick += MenuClick;
            holder.RefreshClick += OnRefreshClick;

            return holder;
        }

        private async void OnRefreshClick(object sender, int position)
        {
            await _authSource.IncrementCounter(position);
            NotifyItemChanged(position);
        }
    }
}