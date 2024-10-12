// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;

namespace Stratum.Droid.Activity
{
    public abstract class AsyncActivity : BaseActivity, IDisposable
    {
        private readonly SemaphoreSlim _onResumeLock;
        private bool _isDisposed;

        protected AsyncActivity(int layout) : base(layout)
        {
            _onResumeLock = new SemaphoreSlim(1, 1);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AsyncActivity()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _onResumeLock.Dispose();
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }

        protected override async void OnResume()
        {
            base.OnResume();

            await _onResumeLock.WaitAsync();

            try
            {
                await OnResumeAsync();
            }
            finally
            {
                _onResumeLock.Release();
            }
        }

        protected abstract Task OnResumeAsync();

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode,
            Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);

            await _onResumeLock.WaitAsync();

            try
            {
                await OnActivityResultAsync(requestCode, resultCode, intent);
            }
            finally
            {
                _onResumeLock.Release();
            }
        }

        protected abstract Task
            OnActivityResultAsync(int requestCode, [GeneratedEnum] Result resultCode, Intent intent);
    }
}