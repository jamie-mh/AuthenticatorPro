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

        private readonly ViewMode _viewMode;
        private readonly bool _isDark;
        private readonly bool _tapToReveal;
        private readonly int _tapToRevealDuration;
        private readonly int _codeGroupSize;
        private readonly bool _showUsernames;

        private readonly IAuthenticatorView _authenticatorView;
        private readonly ICustomIconView _customIconView;

        private readonly Dictionary<int, long> _generationOffsets;
        private readonly Dictionary<int, long> _cooldownOffsets;
        private readonly Queue<int> _positionsToUpdate;
        private readonly Queue<int> _offsetsToUpdate;

        private readonly float _animationScale;

        public AuthenticatorListAdapter(
            Context context, IAuthenticatorView authenticatorView, ICustomIconView customIconView, bool isDark)
        {
            _authenticatorView = authenticatorView;
            _customIconView = customIconView;

            var preferences = new PreferenceWrapper(context);
            _viewMode = ViewModeSpecification.FromName(preferences.ViewMode);
            _tapToReveal = preferences.TapToReveal;
            _tapToRevealDuration = preferences.TapToRevealDuration;
            _codeGroupSize = preferences.CodeGroupSize;
            _showUsernames = preferences.ShowUsernames;
            _isDark = isDark;

            _generationOffsets = new Dictionary<int, long>();
            _cooldownOffsets = new Dictionary<int, long>();
            _positionsToUpdate = new Queue<int>();
            _offsetsToUpdate = new Queue<int>();

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
        }

        public void OnMovementFinished(bool orderChanged)
        {
            MovementFinished?.Invoke(this, orderChanged);
        }

        public void OnMovementStarted()
        {
            MovementStarted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<string> ItemClicked;
        public event EventHandler<string> MenuClicked;
        public event EventHandler<string> RefreshClicked;

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
                    holder.RefreshButton.Visibility = ViewStates.Gone;
                    holder.ProgressIndicator.Visibility = ViewStates.Visible;

                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var offset = GetGenerationOffset(auth.Period);
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
            var payload = (TimerPartialUpdate) payloads[0];
            var offset = GetGenerationOffset(auth.Period);

            if (payload.RequiresGeneration)
            {
                UpdateTimeGeneratorCodeText(auth, holder, offset);
            }

            UpdateProgressIndicator(holder.ProgressIndicator, auth.Period, offset, payload.CurrentOffset);
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
            var offset = GetGenerationOffset(auth.Period);
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

            var noAnimationProgressUpdate
                = new TimerPartialUpdate { CurrentOffset = now, RequiresGeneration = false };

            for (var i = 0; i < ItemCount; ++i)
            {
                var auth = _authenticatorView[i];

                if (auth.Type.GetGenerationMethod() != GenerationMethod.Time)
                {
                    continue;
                }

                var offset = GetGenerationOffset(auth.Period);
                var isExpired = offset + auth.Period <= now;

                if (!isExpired)
                {
                    if (_animationScale.Equals(0f) && ValidatePosition(i))
                    {
                        NotifyItemChanged(i, noAnimationProgressUpdate);
                    }

                    continue;
                }

                _positionsToUpdate.Enqueue(i);

                if (!_offsetsToUpdate.Contains(auth.Period))
                {
                    _offsetsToUpdate.Enqueue(auth.Period);
                }
            }

            while (_offsetsToUpdate.Count > 0)
            {
                UpdateGenerationOffset(_offsetsToUpdate.Dequeue(), now);
            }

            var expiredCodeUpdate = new TimerPartialUpdate { CurrentOffset = now, RequiresGeneration = true };

            while (_positionsToUpdate.Count > 0)
            {
                var position = _positionsToUpdate.Dequeue();

                if (ValidatePosition(position))
                {
                    NotifyItemChanged(position, expiredCodeUpdate);
                }
            }
        }

        private long GetGenerationOffset(int period)
        {
            if (_generationOffsets.TryGetValue(period, out var offset))
            {
                return offset;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateGenerationOffset(period, now);
            return _generationOffsets[period];
        }

        private void UpdateGenerationOffset(int period, long now)
        {
            var offset = now - now % period;
            _generationOffsets[period] = offset;
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
            animator.Start();
        }

        private void UpdateTimeGeneratorCodeText(Authenticator auth, AuthenticatorListHolder holder, long offset)
        {
            var isRevealed = !_tapToReveal ||
                             (_tapToReveal && _cooldownOffsets.ContainsKey(holder.BindingAdapterPosition));
            var code = isRevealed ? auth.GetCode(offset) : null;
            holder.Code.Text = CodeUtil.PadCode(code, auth.Digits, _codeGroupSize);
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
                ItemClicked?.Invoke(this, auth.Secret);
                return;
            }

            if (_cooldownOffsets.Remove(holder.BindingAdapterPosition))
            {
                holder.Code.Text = CodeUtil.PadCode(null, auth.Digits, _codeGroupSize);
            }
            else
            {
                var offset = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _cooldownOffsets[holder.BindingAdapterPosition] = offset + _tapToRevealDuration;
                holder.Code.Text = CodeUtil.PadCode(auth.GetCode(offset), auth.Digits, _codeGroupSize);
                ItemClicked?.Invoke(this, auth.Secret);
            }
        }

        private void OnRefreshClick(int position)
        {
            if (!ValidatePosition(position))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _cooldownOffsets[position] = now + CounterCooldownSeconds;

            var auth = _authenticatorView[position];
            RefreshClicked?.Invoke(this, auth.Secret);
        }

        private class TimerPartialUpdate : Object
        {
            public bool RequiresGeneration { get; set; }
            public long CurrentOffset { get; set; }
        }
    }
}