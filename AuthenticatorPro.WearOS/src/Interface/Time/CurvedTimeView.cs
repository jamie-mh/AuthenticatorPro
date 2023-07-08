// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Wear.Widget;
using System;

namespace AuthenticatorPro.WearOS.Interface.Time
{
    public class CurvedTimeView : CurvedTextView, ITimeView
    {
        private readonly TimeViewHandler _timeViewHandler;
        
        protected CurvedTimeView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            _timeViewHandler = new TimeViewHandler(this);
        }

        public CurvedTimeView(Context context) : base(context)
        {
            _timeViewHandler = new TimeViewHandler(this);
        }

        public CurvedTimeView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            _timeViewHandler = new TimeViewHandler(this);
        }

        public CurvedTimeView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            _timeViewHandler = new TimeViewHandler(this);
        }

        public CurvedTimeView(Context context, IAttributeSet attrs, int defStyle, int defStyleRes) : base(context, attrs, defStyle, defStyleRes)
        {
            _timeViewHandler = new TimeViewHandler(this);
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            _timeViewHandler.Start();
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            _timeViewHandler.Stop();
        }
    }
}