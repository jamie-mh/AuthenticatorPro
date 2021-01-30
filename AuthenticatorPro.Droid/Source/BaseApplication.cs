using System;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Lifecycle;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Util;
using Java.Interop;

namespace AuthenticatorPro.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    internal class BaseApplication : Application, ILifecycleObserver
    {
        public bool IsLocked { get; private set; }
        public bool PreventNextLock { get; set; }
        
        private Timer _timeoutTimer;
        private PreferenceWrapper _preferences;
       
        
        public BaseApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidEnvironmentUnhandledExceptionRaised;
            
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
            _preferences = new PreferenceWrapper(Context);
            IsLocked = true;
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

        private void HandleException(Exception exception)
        {
            Logger.Error(exception);
            var intent = new Intent(this, typeof(ErrorActivity));
            intent.PutExtra("exception", exception.ToString());
            StartActivity(intent);
        }

        [Lifecycle.Event.OnStop]
        [Export]
        public async void OnStopped()
        {
            if(!_preferences.PasswordProtected)
                return;

            if(PreventNextLock)
            {
                PreventNextLock = false;
                return;
            }

            var timeout = _preferences.Timeout;
            
            if(timeout == 0)
                await Lock();
            else
            {
                _timeoutTimer = new Timer(timeout * 1000);
                _timeoutTimer.Elapsed += async delegate
                {
                    await Lock();
                };
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
            await Lock();
        }

        public async Task Unlock(string password)
        {
            await Database.OpenSharedConnection(password);
            IsLocked = false;
        }

        public async Task Lock()
        {
            await Database.CloseSharedConnection();
            IsLocked = true;
        }
    }
}