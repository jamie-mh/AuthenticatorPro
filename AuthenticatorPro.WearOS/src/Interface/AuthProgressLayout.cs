// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Timers;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.SwipeRefreshLayout.Widget;

namespace AuthenticatorPro.WearOS.Interface
{
    internal sealed class AuthProgressLayout : FrameLayout, ViewGroup.IOnHierarchyChangeListener
    {
        private const float StartingRotation = .75f;
        private const long TimerInterval = 1000 / 60;
        private const float StrokeWidth = 6f;

        private readonly CircularProgressDrawable _progressDrawable;
        private readonly Timer _timer;

        private long _timeSinceStart;


        private AuthProgressLayout(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public AuthProgressLayout(Context context) : this(context, null)
        {
        }

        public AuthProgressLayout(Context context, IAttributeSet attrs) : this(context, attrs, 0)
        {
        }

        public AuthProgressLayout(Context context, IAttributeSet attrs, int defStyleAttr) : this(context, attrs,
            defStyleAttr, 0)
        {
        }

        public AuthProgressLayout(
            Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes)
            : base(context, attrs, defStyleAttr, defStyleRes)
        {
            _progressDrawable = new CircularProgressDrawable(context);
            _progressDrawable.ProgressRotation = StartingRotation;
            _progressDrawable.StrokeCap = Paint.Cap.Butt;
            _progressDrawable.StrokeWidth = StrokeWidth;

            var accentColour = ContextCompat.GetColor(context, Resource.Color.colorAccent);
            _progressDrawable.SetColorSchemeColors(accentColour);

            _timer = new Timer { Interval = TimerInterval, AutoReset = true };
            _timer.Elapsed += OnTimerElapsed;

            Background = _progressDrawable;
        }

        public long Period { get; set; }

        public void OnChildViewAdded(View parent, View child)
        {
            var frameLayoutParams = (LayoutParams) child.LayoutParameters;
            frameLayoutParams.Gravity = GravityFlags.Center;
            child.LayoutParameters = frameLayoutParams;
        }

        public void OnChildViewRemoved(View parent, View child)
        {
        }

        public event EventHandler<ElapsedEventArgs> TimerFinished;

        private void DrawProgress()
        {
            var timeRemaining = Period - _timeSinceStart;
            var progress = 1f - (float) timeRemaining / Period;
            _progressDrawable.SetStartEndTrim(0f, progress);
            Invalidate();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timeSinceStart += TimerInterval;

            if (_timeSinceStart >= Period)
            {
                _timeSinceStart = 0;
                TimerFinished?.Invoke(this, e);
            }

            DrawProgress();
        }

        public void StartTimer(long startingTime = 0)
        {
            if (_timer.Enabled)
            {
                StopTimer();
            }

            _timeSinceStart = startingTime;
            _timer.Start();
            DrawProgress();
        }

        public void StopTimer()
        {
            if (!_timer.Enabled)
            {
                return;
            }

            _progressDrawable.SetStartEndTrim(0f, 0f);
            _timer.Stop();
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            if (ChildCount == 0)
            {
                _progressDrawable.CenterRadius = 0f;
                return;
            }

            var child = GetChildAt(0);
            _progressDrawable.CenterRadius = Math.Min(child.Width, child.Height) / 2f;
        }
    }
}