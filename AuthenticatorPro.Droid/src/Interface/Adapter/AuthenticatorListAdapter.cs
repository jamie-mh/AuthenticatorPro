// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Android.Animation;
using Android.Content;
using Android.Provider;
using Android.Views;
using Android.Views.Animations;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Shared;
using Google.Android.Material.ProgressIndicator;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    public class AuthenticatorListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        private const int MaxProgress = 10000;
        private const int CounterCooldownSeconds = 10;
        private const double SkipToNextRatio = 1d / 3d;

        private readonly ViewMode _viewMode;
        private readonly bool _isDark;
        private readonly bool _tapToCopy;
        private readonly bool _tapToReveal;
        private readonly int _tapToRevealDuration;
        private readonly int _codeGroupSize;
        private readonly bool _showUsernames;
        private readonly bool _skipToNext;

        private readonly IAuthenticatorView _authenticatorView;
        private readonly ICustomIconView _customIconView;

        private readonly Dictionary<int, long> _generationOffsets;
        private readonly Dictionary<int, long> _cooldownOffsets;

        private readonly float _animationScale;

        public AuthenticatorListAdapter(
            Context context, IAuthenticatorView authenticatorView, ICustomIconView customIconView, bool isDark)
        {
            _authenticatorView = authenticatorView;
            _customIconView = customIconView;

            var preferences = new PreferenceWrapper(context);
            _viewMode = ViewModeSpecification.FromName(preferences.ViewMode);
            _tapToCopy = preferences.TapToCopy;
            _tapToReveal = preferences.TapToReveal;
            _tapToRevealDuration = preferences.TapToRevealDuration;
            _codeGroupSize = preferences.CodeGroupSize;
            _showUsernames = preferences.ShowUsernames;
            _skipToNext = preferences.SkipToNext;
            _isDark = isDark;

            _generationOffsets = new Dictionary<int, long>();
            _cooldownOffsets = new Dictionary<int, long>();

            _animationScale =
                Settings.Global.GetFloat(context.ContentResolver, Settings.Global.AnimatorDurationScale, 1.0f);
        }

        public override int ItemCount => _authenticatorView.Count;

        public void MoveItemView(int oldPosition, int newPosition)
        {
            if (!ValidatePosition(oldPosition) || !ValidatePosition(newPosition))
            {
                return;
            }

            _authenticatorView.Swap(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);

            SwapDictionaryValues(_generationOffsets, oldPosition, newPosition);
            SwapDictionaryValues(_cooldownOffsets, oldPosition, newPosition);
        }

        private static void SwapDictionaryValues<T>(Dictionary<int, T> dictionary, int oldPosition, int newPosition)
        {
            if (dictionary.TryGetValue(oldPosition, out var oldValue))
            {
                if (dictionary.TryGetValue(newPosition, out var newValue))
                {
                    dictionary[oldPosition] = newValue;
                }
                
                dictionary[newPosition] = oldValue;
            }
            else if (dictionary.TryGetValue(newPosition, out var newValue))
            {
                dictionary[oldPosition] = newValue;
            }
        }

        public void OnMovementFinished(bool orderChanged)
        {
            MovementFinished?.Invoke(this, orderChanged);
        }

        public void OnMovementStarted()
        {
            MovementStarted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<string> CodeCopied;
        public event EventHandler<string> MenuClicked;
        public event EventHandler<string> IncrementCounterClicked;

        public event EventHandler MovementStarted;
        public event EventHandler<bool> MovementFinished;

        public override long GetItemId(int position)
        {
            return ValidatePosition(position)
                ? _authenticatorView[position].Secret.GetHashCode()
                : RecyclerView.NoId;
        }

        private bool ValidatePosition(int position)
        {
            return position >= 0 && position < _authenticatorView.Count;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (!ValidatePosition(position))
            {
                return;
            }

            var auth = _authenticatorView[position];
            var holder = (AuthenticatorListHolder) viewHolder;

            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            if (!_showUsernames)
            {
                var uniqueIssuer = _authenticatorView.Count(a => a.Issuer == auth.Issuer) == 1;
                holder.Username.Visibility = uniqueIssuer
                    ? ViewStates.Gone
                    : ViewStates.Visible;
            }
            else
            {
                holder.Username.Visibility = string.IsNullOrEmpty(auth.Username)
                    ? ViewStates.Gone
                    : ViewStates.Visible;
            }

            if (auth.Icon != null && auth.Icon.StartsWith(CustomIcon.Prefix))
            {
                var id = auth.Icon[1..];
                var customIconBitmap = _customIconView.GetOrDefault(id);

                if (customIconBitmap != null)
                {
                    holder.Icon.SetImageBitmap(customIconBitmap);
                }
                else
                {
                    holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, _isDark));
                }
            }
            else
            {
                holder.Icon.SetImageResource(IconResolver.GetService(auth.Icon, _isDark));
            }

            switch (auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                {
                    holder.ProgressIndicator.Visibility = ViewStates.Visible;
                    holder.RefreshButton.Visibility = ViewStates.Gone;

                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var offset = GetGenerationOffset(holder.BindingAdapterPosition, auth.Period);
                    
                    UpdateTimeGeneratorCodeText(auth, holder, offset);
                    UpdateProgressIndicator(holder.ProgressIndicator, auth.Period, offset, now);
                    break;
                }

                case GenerationMethod.Counter:
                {
                    var inCooldown = _cooldownOffsets.ContainsKey(position);
                    var code = (_tapToReveal && inCooldown) || !_tapToReveal ? auth.GetCode() : null;

                    holder.Code.Text = CodeUtil.PadCode(code, auth.Digits, _codeGroupSize);
                    holder.RefreshButton.Visibility = inCooldown ? ViewStates.Invisible : ViewStates.Visible;
                    holder.ProgressIndicator.Visibility = ViewStates.Invisible;
                    break;
                }
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position, IList<Object> payloads)
        {
            if (payloads == null || payloads.Count == 0)
            {
                OnBindViewHolder(viewHolder, position);
                return;
            }

            if (!ValidatePosition(position))
            {
                return;
            }

            var auth = _authenticatorView[position];

            if (auth.Type.GetGenerationMethod() != GenerationMethod.Time)
            {
                return;
            }

            var holder = (AuthenticatorListHolder) viewHolder;
            var payload = (PartialUpdate) payloads[0];
            var offset = GetGenerationOffset(holder.BindingAdapterPosition, auth.Period);

            if (payload.RequiresGeneration)
            {
                UpdateTimeGeneratorCodeText(auth, holder, offset);
            }

            if (payload.RequiresGeneration || _animationScale.Equals(0f))
            {
                UpdateProgressIndicator(holder.ProgressIndicator, auth.Period, offset, payload.CurrentOffset);
            }

            holder.RefreshButton.Visibility = payload.ShowRefresh ? ViewStates.Visible : ViewStates.Gone;
        }

        public override void OnViewAttachedToWindow(Object holderObj)
        {
            base.OnViewAttachedToWindow(holderObj);

            var holder = (AuthenticatorListHolder) holderObj;

            if (holder.BindingAdapterPosition == RecyclerView.NoPosition)
            {
                return;
            }

            var auth = _authenticatorView[holder.BindingAdapterPosition];

            if (auth.Type.GetGenerationMethod() != GenerationMethod.Time)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var offset = GetGenerationOffset(holder.BindingAdapterPosition, auth.Period);
            var showRefreshButton = false;

            if (_skipToNext)
            {
                showRefreshButton = ShouldAllowSkip(offset, auth.Period, now, holder.BindingAdapterPosition);
            }
            
            holder.RefreshButton.Visibility = showRefreshButton ? ViewStates.Visible : ViewStates.Gone;
            UpdateProgressIndicator(holder.ProgressIndicator, auth.Period, offset, now);
            UpdateTimeGeneratorCodeText(auth, holder, offset);
        }

        public void Tick()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var (position, offset) in _cooldownOffsets.ToImmutableArray())
            {
                if (offset > now)
                {
                    continue;
                }

                if (ValidatePosition(position))
                {
                    NotifyItemChanged(position);
                }

                _cooldownOffsets.Remove(position);
            }

            for (var i = 0; i < ItemCount; ++i)
            {
                var auth = _authenticatorView[i];

                if (auth.Type.GetGenerationMethod() != GenerationMethod.Time)
                {
                    continue;
                }

                var offset = GetGenerationOffset(i, auth.Period);
                var renewOffset = offset + auth.Period;
                var isExpired = renewOffset <= now;
                var showRefresh = false;

                if (isExpired)
                {
                    UpdateGenerationOffset(i, auth.Period, now);
                }
                else if (_skipToNext)
                {
                    showRefresh = ShouldAllowSkip(offset, auth.Period, now, i);
                }

                if ((isExpired || showRefresh || _animationScale.Equals(0f)) &&
                    ValidatePosition(i))
                {
                    var update = new PartialUpdate
                    {
                        CurrentOffset = now,
                        RequiresGeneration = isExpired,
                        ShowRefresh = showRefresh
                    };
                    
                    NotifyItemChanged(i, update);
                }
            }
        }

        private long GetGenerationOffset(int position, int period)
        {
            if (_generationOffsets.TryGetValue(position, out var offset))
            {
                return offset;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateGenerationOffset(position, period, now);
            return _generationOffsets[position];
        }

        private void UpdateGenerationOffset(int position, int period, long now)
        {
            var offset = now - now % period;
            _generationOffsets[position] = offset;
        }

        private void SkipToNextOffset(int position, int period)
        {
            var currentOffset = GetGenerationOffset(position, period);
            UpdateGenerationOffset(position, period, currentOffset + period);
        }

        private void UpdateProgressIndicator(LinearProgressIndicator progressIndicator, int period,
            long generationOffset, long now)
        {
            var renewTime = generationOffset + period;
            var secondsRemaining = Math.Max(renewTime - now, 0);
            var progress = (int) Math.Round((double) MaxProgress * secondsRemaining / period);

            progressIndicator.SetProgressCompat(progress, true);

            if (_animationScale.Equals(0f))
            {
                return;
            }

            var animator = ObjectAnimator.OfInt(progressIndicator, "progress", 0);
            var duration = (int) (secondsRemaining * 1000 / _animationScale);

            animator.SetDuration(duration);
            animator.SetInterpolator(new LinearInterpolator());
            animator.SetAutoCancel(true);
            animator.Start();
        }

        private void UpdateTimeGeneratorCodeText(Authenticator auth, AuthenticatorListHolder holder, long offset)
        {
            var isRevealed = !_tapToReveal ||
                             (_tapToReveal && _cooldownOffsets.ContainsKey(holder.BindingAdapterPosition));
            var code = isRevealed ? auth.GetCode(offset) : null;
            holder.Code.Text = CodeUtil.PadCode(code, auth.Digits, _codeGroupSize);
        }

        private bool ShouldAllowSkip(long offset, int period, long now, int position)
        {
            var renewOffset = offset + period;
            var progress = (double) Math.Max(renewOffset - now, 0) / period;
            return progress < SkipToNextRatio && (!_tapToReveal || _cooldownOffsets.ContainsKey(position));
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
            holder.MenuButton.Click += delegate
            {
                var auth = _authenticatorView[holder.BindingAdapterPosition];
                MenuClicked.Invoke(this, auth.Secret);
            };
            holder.RefreshButton.Click += delegate { OnRefreshClick(holder.BindingAdapterPosition); };

            return holder;
        }

        private void OnItemClick(AuthenticatorListHolder holder)
        {
            if (!ValidatePosition(holder.BindingAdapterPosition))
            {
                return;
            }

            var auth = _authenticatorView[holder.BindingAdapterPosition];
            
            if (!_tapToReveal)
            {
                if (_tapToCopy)
                {
                    CodeCopied?.Invoke(this, auth.Secret);
                }
                
                return;
            }

            var isRevealed = _cooldownOffsets.ContainsKey(holder.BindingAdapterPosition);

            if (isRevealed)
            {
                if (_tapToCopy)
                {
                    CodeCopied?.Invoke(this, auth.Secret);    
                }
                else
                {
                    _cooldownOffsets.Remove(holder.BindingAdapterPosition);
                    holder.Code.Text = CodeUtil.PadCode(null, auth.Digits, _codeGroupSize);

                    if (_skipToNext)
                    {
                        holder.RefreshButton.Visibility = ViewStates.Gone;
                    }
                }
            }
            else
            {
                var offset = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _cooldownOffsets[holder.BindingAdapterPosition] = offset + _tapToRevealDuration;
                holder.Code.Text = CodeUtil.PadCode(auth.GetCode(offset), auth.Digits, _codeGroupSize);
            }
        }

        private void OnRefreshClick(int position)
        {
            if (!ValidatePosition(position))
            {
                return;
            }

            var auth = _authenticatorView[position];
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            switch (auth.Type.GetGenerationMethod())
            {
                case GenerationMethod.Time:
                    if (_cooldownOffsets.ContainsKey(position))
                    {
                        _cooldownOffsets[position] = now + _tapToRevealDuration;
                    }
                    
                    SkipToNextOffset(position, auth.Period);
                    NotifyItemChanged(position);
                    break;

                case GenerationMethod.Counter:
                {
                    _cooldownOffsets[position] = now + CounterCooldownSeconds;
                    IncrementCounterClicked?.Invoke(this, auth.Secret);
                    break;
                }
            }
        }

        public new void NotifyDataSetChanged()
        {
            _generationOffsets.Clear();
            _cooldownOffsets.Clear();
            base.NotifyDataSetChanged(); 
        }

        private class PartialUpdate : Object
        {
            public bool RequiresGeneration { get; set; }
            public bool ShowRefresh { get; set; }
            public long CurrentOffset { get; set; }
        }
    }
}