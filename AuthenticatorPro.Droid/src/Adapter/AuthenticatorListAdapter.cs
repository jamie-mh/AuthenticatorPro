// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Droid.ViewHolder;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Service;
using AuthenticatorPro.Shared.Util;
using AuthenticatorPro.Shared.View;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.Adapter
{
    internal class AuthenticatorListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        private const int MaxProgress = 10000;
        private const int CounterCooldownSeconds = 10;

        public event EventHandler<int> ItemClicked;
        public event EventHandler<int> MenuClicked;

        public event EventHandler MovementStarted;
        public event EventHandler MovementFinished;

        private readonly ViewMode _viewMode;
        private readonly bool _isDark;
        private readonly bool _tapToReveal;
        private readonly int _codeGroupSize;

        private readonly IAuthenticatorService _authenticatorService;
        private readonly IAuthenticatorView _authenticatorView;
        private readonly ICustomIconRepository _customIconRepository;

        private readonly SemaphoreSlim _customIconDecodeLock;
        private readonly Dictionary<string, Bitmap> _decodedCustomIcons;

        private readonly Dictionary<int, long> _generationOffsets;
        private readonly Dictionary<int, long> _counterCooldownOffsets;
        private readonly Queue<int> _positionsToUpdate;
        private readonly Queue<int> _offsetsToUpdate;

        private readonly float _animationScale;

        public AuthenticatorListAdapter(Context context, IAuthenticatorService authenticatorService,
            IAuthenticatorView authenticatorView, ICustomIconRepository customIconRepository, bool isDark)
        {
            _authenticatorService = authenticatorService;
            _authenticatorView = authenticatorView;
            _customIconRepository = customIconRepository;

            var preferences = new PreferenceWrapper(context);
            _viewMode = ViewModeSpecification.FromName(preferences.ViewMode);
            _tapToReveal = preferences.TapToReveal;
            _codeGroupSize = preferences.CodeGroupSize;
            _isDark = isDark;

            _customIconDecodeLock = new SemaphoreSlim(1, 1);
            _decodedCustomIcons = new Dictionary<string, Bitmap>();

            _generationOffsets = new Dictionary<int, long>();
            _counterCooldownOffsets = new Dictionary<int, long>();
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

        public void OnMovementFinished()
        {
            MovementFinished?.Invoke(this, EventArgs.Empty);
        }

        public void OnMovementStarted()
        {
            MovementStarted?.Invoke(this, null);
        }

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

        public override async void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            if (!ValidatePosition(position))
            {
                return;
            }

            var auth = _authenticatorView[position];
            var holder = (AuthenticatorListHolder) viewHolder;

            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            holder.Username.Visibility = String.IsNullOrEmpty(auth.Username)
                ? ViewStates.Gone
                : ViewStates.Visible;

            if (auth.Icon != null && auth.Icon.StartsWith(CustomIcon.Prefix))
            {
                var id = auth.Icon[1..];
                Bitmap decoded;

                if ((decoded = await DecodeCustomIcon(id)) != null)
                {
                    holder.Icon.SetImageBitmap(decoded);
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
                    if (_tapToReveal)
                    {
                        holder.Code.Text = CodeUtil.PadCode(null, auth.Digits, _codeGroupSize);
                    }

                    holder.RefreshButton.Visibility = ViewStates.Gone;
                    holder.ProgressBar.Visibility = ViewStates.Visible;

                    var offset = GetGenerationOffset(auth.Period);
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    UpdateProgressBar(holder.ProgressBar, auth.Period, offset, now);
                    break;
                }

                case GenerationMethod.Counter:
                {
                    var inCooldown = _counterCooldownOffsets.ContainsKey(position);
                    var code = (_tapToReveal && inCooldown) || !_tapToReveal ? auth.GetCode() : null;

                    holder.Code.Text = CodeUtil.PadCode(code, auth.Digits, _codeGroupSize);
                    holder.RefreshButton.Visibility = inCooldown ? ViewStates.Invisible : ViewStates.Visible;
                    holder.ProgressBar.Visibility = ViewStates.Invisible;
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
                var code = _tapToReveal ? null : auth.GetCode(offset);
                holder.Code.Text = CodeUtil.PadCode(code, auth.Digits, _codeGroupSize);
            }

            UpdateProgressBar(holder.ProgressBar, auth.Period, offset, payload.CurrentOffset);
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

            var offset = GetGenerationOffset(auth.Period);
            var code = _tapToReveal ? null : auth.GetCode(offset);
            holder.Code.Text = CodeUtil.PadCode(code, auth.Digits, _codeGroupSize);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            UpdateProgressBar(holder.ProgressBar, auth.Period, offset, now);
        }

        private async Task<Bitmap> DecodeCustomIcon(string id)
        {
            if (_decodedCustomIcons.TryGetValue(id, out var bitmap))
            {
                return bitmap;
            }

            CustomIcon icon;

            try
            {
                icon = await _customIconRepository.GetAsync(id);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }

            if (icon == null)
            {
                return null;
            }

            await _customIconDecodeLock.WaitAsync();

            try
            {
                if (_decodedCustomIcons.TryGetValue(id, out bitmap))
                {
                    return bitmap;
                }

                bitmap = await BitmapFactory.DecodeByteArrayAsync(icon.Data, 0, icon.Data.Length);
                _decodedCustomIcons.Add(icon.Id, bitmap);
                return bitmap;
            }
            catch (Exception e)
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

            foreach (var (position, offset) in _counterCooldownOffsets.ToImmutableArray())
            {
                if (offset > now)
                {
                    continue;
                }

                NotifyItemChanged(position);
                _counterCooldownOffsets.Remove(position);
            }

            var timerUpdate = new TimerPartialUpdate { CurrentOffset = now, RequiresGeneration = false };

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
                    if (_animationScale == 0)
                    {
                        NotifyItemChanged(i, timerUpdate);
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

            timerUpdate.RequiresGeneration = true;

            while (_positionsToUpdate.Count > 0)
            {
                NotifyItemChanged(_positionsToUpdate.Dequeue(), timerUpdate);
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
            var offset = now - (now % period);
            _generationOffsets[period] = offset;
        }

        private void UpdateProgressBar(ProgressBar progressBar, int period, long generationOffset, long now)
        {
            var renewTime = generationOffset + period;
            var secondsRemaining = Math.Max(renewTime - now, 0);
            var progress = (int) Math.Round((double) MaxProgress * secondsRemaining / period);

            progressBar.Progress = progress;

            if (_animationScale == 0)
            {
                return;
            }

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
            holder.MenuButton.Click += delegate { MenuClicked.Invoke(this, holder.BindingAdapterPosition); };
            holder.RefreshButton.Click += delegate { OnRefreshClick(holder.BindingAdapterPosition); };

            return holder;
        }

        private void OnItemClick(AuthenticatorListHolder holder)
        {
            if (!ValidatePosition(holder.BindingAdapterPosition))
            {
                return;
            }

            if (_tapToReveal)
            {
                var auth = _authenticatorView[holder.BindingAdapterPosition];
                var offset = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                holder.Code.Text = CodeUtil.PadCode(auth.GetCode(offset), auth.Digits, _codeGroupSize);
            }

            ItemClicked?.Invoke(this, holder.BindingAdapterPosition);
        }

        private async void OnRefreshClick(int position)
        {
            if (!ValidatePosition(position))
            {
                return;
            }

            var auth = _authenticatorView[position];
            await _authenticatorService.IncrementCounterAsync(auth);

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