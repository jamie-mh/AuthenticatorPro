// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Wear.Widget;
using JetBrains.Annotations;
using System;
using System.Timers;

namespace AuthenticatorPro.WearOS.Interface
{
    public class CurrentTimeView : CurvedTextView
    {
        private Timer _timer;
        
        protected CurrentTimeView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            Init();
        }

        public CurrentTimeView([NotNull] Context context) : base(context)
        {
            Init();
        }

        public CurrentTimeView([NotNull] Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init();
        }

        public CurrentTimeView([NotNull] Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Init();
        }

        public CurrentTimeView([NotNull] Context context, IAttributeSet attrs, int defStyle, int defStyleRes) : base(context, attrs, defStyle, defStyleRes)
        {
            Init();
        }

        private void Init()
        {
            _timer = new Timer { Interval = 1000, AutoReset = true };
            _timer.Elapsed += delegate { UpdateTime(); };
        }

        private void UpdateTime()
        {
            Text = DateTime.Now.ToString("H:mm");
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            UpdateTime();
            _timer.Start();
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            _timer.Stop();
        }
    }
}