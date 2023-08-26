// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Lifecycle;
using AuthenticatorPro.Droid.Activity;
using Java.Interop;

namespace AuthenticatorPro.Droid
{
#if DEBUG
    [Application(Debuggable = true, TaskAffinity = "")]
#else
    [Application(Debuggable = false, TaskAffinity = "")]
#endif
    public class BaseApplication : Application, ILifecycleObserver
    {
        public bool AutoLockEnabled { get; set; }
        public bool PreventNextAutoLock { get; set; }

        private readonly Database _database;
        private Timer _timeoutTimer;
        private PreferenceWrapper _preferences;

        public BaseApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            _database = new Database();

            Dependencies.Register(_database);
            Dependencies.RegisterApplicationContext(this);

            AutoLockEnabled = false;
            PreventNextAutoLock = false;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidEnvironmentUnhandledExceptionRaised;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
            _preferences = new PreferenceWrapper(Context);

            if (_preferences.FirstLaunch)
            {
                _preferences.DynamicColour = Build.VERSION.SdkInt >= BuildVersionCodes.S;
            }
        }

        private void OnAndroidEnvironmentUnhandledExceptionRaised(object sender, RaiseThrowableEventArgs e)
        {
            e.Handled = true;
            HandleException(e.Exception);
        }

        private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;
            HandleException(exception);
        }

        private void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            HandleException(e.Exception);
        }

        private void HandleException(Exception exception)
        {
            Logger.Error(exception);
            var intent = new Intent(this, typeof(ErrorActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            intent.PutExtra("exception", exception.ToString());
            StartActivity(intent);
        }

        [Lifecycle.Event.OnStop]
        [Export]
        public async void OnStopped()
        {
            if (!AutoLockEnabled)
            {
                return;
            }

            if (PreventNextAutoLock)
            {
                PreventNextAutoLock = false;
                return;
            }

            if (!_preferences.PasswordProtected || _preferences.Timeout == 0)
            {
                await _database.CloseAsync(Database.Origin.Application);
            }
            else
            {
                _timeoutTimer = new Timer(_preferences.Timeout * 1000) { AutoReset = false };
                _timeoutTimer.Elapsed += async delegate { await _database.CloseAsync(Database.Origin.Application); };
                _timeoutTimer.Start();
            }
        }

        [Lifecycle.Event.OnStart]
        [Export]
        public void OnStarted()
        {
            _timeoutTimer?.Stop();
        }

        [Lifecycle.Event.OnDestroy]
        [Export]
        public async void OnDestroyed()
        {
            await _database.CloseAsync(Database.Origin.Application);
        }
    }
}