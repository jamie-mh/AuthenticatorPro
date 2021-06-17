// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Util;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.List
{
    internal sealed class AuthenticatorListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        private const int MaxProgress = 10000;
        private const int CounterCooldownSeconds = 10;
        private const int RevealLength = 4000;

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> MenuClick;

        public event EventHandler MovementStarted;
        public event EventHandler MovementFinished;

        private readonly ViewMode _viewMode;
        private readonly bool _isDark;
        private readonly bool _tapToReveal;
        
        private readonly AuthenticatorSource _authSource;
        private readonly CustomIconSource _customIconSource;

        private readonly SemaphoreSlim _customIconDecodeLock;
        private readonly Dictionary<string, Bitmap> _decodedCustomIcons;
        
        private readonly Dictionary<int, long> _generationOffsets;
        private readonly Dictionary<int, long> _counterCooldownOffsets;

        private readonly float _animationScale;
        
        public AuthenticatorListAdapter(Context context, AuthenticatorSource authSource, CustomIconSource customIconSource, ViewMode viewMode, bool isDark, bool tapToReveal)
        {
            _authSource = authSource;
            _customIconSource = customIconSource;
            _viewMode = viewMode;
            _isDark = isDark;
            _tapToReveal = tapToReveal;

            _customIconDecodeLock = new SemaphoreSlim(1, 1);
            _decodedCustomIcons = new Dictionary<string, Bitmap>();
            
            _generationOffsets = new Dictionary<int, long>();
            _counterCooldownOffsets = new Dictionary<int, long>();
            
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
            catch(Exception e)
            {
                // Cannot revert, keep going
                Logger.Error(e);
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

            if(auth.Icon != null && auth.Icon.StartsWith(CustomIcon.Prefix))
            {
                var id = auth.Icon[1..];
                var customIcon = _customIconSource.Get(id);
                Bitmap decoded;

                if(customIcon != null && (decoded = await DecodeCustomIcon(customIcon)) != null)
                    holder.Icon.SetImageBitmap(decoded); 
                else
                    holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, _isDark));
            }
            else
                holder.Icon.SetImageResource(IconResolver.GetService(auth.Icon, _isDark));

            switch(auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                {
                    if(_tapToReveal)
                        holder.Code.Text = CodeUtil.PadCode(null, auth.Digits);
                    
                    holder.RefreshButton.Visibility = ViewStates.Gone;
                    holder.ProgressBar.Visibility = ViewStates.Visible;
                    break;
                }

                case GenerationMethod.Counter:
                {
                    var inCooldown = _counterCooldownOffsets.ContainsKey(position);

                    if(_tapToReveal)
                    {
                        var code = inCooldown ? auth.GetCode() : null;
                        holder.Code.Text = CodeUtil.PadCode(code, auth.Digits);
                    }
                    
                    holder.RefreshButton.Visibility = inCooldown ? ViewStates.Invisible : ViewStates.Visible;
                    holder.ProgressBar.Visibility = ViewStates.Invisible;
                    break;
                }
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

            if(auth.Type.GetGenerationMethod() != GenerationMethod.Time)
                return;
            
            var holder = (AuthenticatorListHolder) viewHolder;
            var payload = (TimerPartialUpdate) payloads[0];
            var offset = GetGenerationOffset(auth.Period);
            
            if(payload.RequiresGeneration)
            {
                var code = _tapToReveal ? null : auth.GetCode(offset);
                holder.Code.Text = CodeUtil.PadCode(code, auth.Digits);
            }
            
            UpdateProgressBar(holder.ProgressBar, auth.Period, offset, payload.CurrentOffset);
        }

        public override void OnViewAttachedToWindow(Object holderObj)
        {
            base.OnViewAttachedToWindow(holderObj);
            
            var holder = (AuthenticatorListHolder) holderObj;
            var auth = _authSource.Get(holder.AdapterPosition);

            if(auth == null || auth.Type.GetGenerationMethod() != GenerationMethod.Time)
                return;
            
            var offset = GetGenerationOffset(auth.Period);
            var code = _tapToReveal ? null : auth.GetCode(offset);
            holder.Code.Text = CodeUtil.PadCode(code, auth.Digits);
            
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateProgressBar(holder.ProgressBar, auth.Period, offset, now);
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
            catch(Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                _customIconDecodeLock.Release();
            }
        }

        public void Tick()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach(var (position, offset) in _counterCooldownOffsets.ToImmutableArray())
            {
                if(offset > now)
                    continue;
                    
                NotifyItemChanged(position);
                _counterCooldownOffsets.Remove(position);
            }
            
            var timerUpdate = new TimerPartialUpdate
            {
                CurrentOffset = now,
                RequiresGeneration = false
            };

            var positionsToUpdate = new List<int>(ItemCount);
            var offsetsToUpdate = new List<int>(_generationOffsets.Count);

            for(var i = 0; i < ItemCount; ++i)
            {
                var auth = _authSource.Get(i);

                if(auth == null || auth.Type.GetGenerationMethod() != GenerationMethod.Time)
                    continue;

                var offset = GetGenerationOffset(auth.Period);
                var isExpired = offset + auth.Period <= now;

                if(!isExpired)
                {
                    if(_animationScale == 0)
                        NotifyItemChanged(i, timerUpdate);
                    
                    continue;
                }

                positionsToUpdate.Add(i);
                offsetsToUpdate.Add(auth.Period);
            }
            
            foreach(var offset in offsetsToUpdate)
                UpdateGenerationOffset(offset, now);

            timerUpdate.RequiresGeneration = true;
            
            foreach(var i in positionsToUpdate)
                NotifyItemChanged(i, timerUpdate);
        }

        private long GetGenerationOffset(int period)
        {
            if(_generationOffsets.TryGetValue(period, out var offset))
                return offset;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateGenerationOffset(period, now);
            return _generationOffsets[period];
        }

        private void UpdateGenerationOffset(int period, long now)
        {
            var offset = now - now % period;
            _generationOffsets[period] = offset;
        }
        
        private void UpdateProgressBar(ProgressBar progressBar, int period, long generationOffset, long now)
        {
            var renewTime = generationOffset + period;
            var secondsRemaining = Math.Max(renewTime - now, 0);
            var progress = (int) Math.Round((double) MaxProgress * secondsRemaining / period);
            
            progressBar.Progress = progress;
            
            if(_animationScale == 0)
                return;
            
            var animator = ObjectAnimator.OfInt(progressBar, "progress", 0);
            var duration = (int) (secondsRemaining * 1000 / _animationScale);
            
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
            holder.ItemView.Click += delegate { OnItemClick(holder); };
            holder.MenuButton.Click += delegate { MenuClick.Invoke(this, holder.AdapterPosition); };
            holder.RefreshButton.Click += delegate { OnRefreshClick(holder.AdapterPosition); };

            return holder;
        }

        private void OnItemClick(AuthenticatorListHolder holder)
        {
            if(_tapToReveal)
            {
                var auth = _authSource.Get(holder.AdapterPosition);

                if(auth != null)
                {
                    var offset = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    holder.Code.Text = CodeUtil.PadCode(auth.GetCode(offset), auth.Digits);
                }
            }
            
            ItemClick?.Invoke(this, holder.AdapterPosition); 
        }

        private async void OnRefreshClick(int position)
        {
            await _authSource.IncrementCounter(position);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _counterCooldownOffsets[position] = now + CounterCooldownSeconds;

            NotifyItemChanged(position);
        }

        private class TimerPartialUpdate : Object
        {
            public bool RequiresGeneration { get; set; }
            public long CurrentOffset { get; set; }
        }
    }
}