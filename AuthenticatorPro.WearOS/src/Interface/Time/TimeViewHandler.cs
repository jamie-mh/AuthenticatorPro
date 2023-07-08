// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Timers;

namespace AuthenticatorPro.WearOS.Interface.Time
{
    public class TimeViewHandler
    {
        private readonly ITimeView _timeView;
        private readonly Timer _timer;

        public TimeViewHandler(ITimeView timeView)
        {
            _timeView = timeView;
            _timer = new Timer { Interval = 1000, AutoReset = true };
            _timer.Elapsed += delegate { UpdateTime(); };
        }
        
        private void UpdateTime()
        {
            _timeView.Text = DateTime.Now.ToString("H:mm");
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Start()
        {
            _timer.Start();
            UpdateTime();
        }
    }
}