using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Lifecycle;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Util;
using Java.Interop;
using Timer = System.Timers.Timer;

namespace AuthenticatorPro.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    internal class BaseApplication : Application, ILifecycleObserver
    {
        private int _isLocked;
        public bool IsLocked => Interlocked.CompareExchange(ref _isLocked, 0, 0) == 1;
        
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
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
            
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
            _preferences = new PreferenceWrapper(Context);
            Interlocked.Exchange(ref _isLocked, 1);
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
            if(PreventNextLock)
            {
                PreventNextLock = false;
                return;
            }

            int timeout;
            
            if(!_preferences.PasswordProtected || (timeout = _preferences.Timeout) == 0)
                await Lock();
            else
            {
                _timeoutTimer = new Timer(timeout * 1000)
                {
                    AutoReset = false
                };
                
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
            if(IsLocked)
                await Lock();
            
            await Database.OpenSharedConnection(password);
            Interlocked.Exchange(ref _isLocked, 0);
        }

        public async Task Lock()
        {
            if(IsLocked)
                return;
            
            await Database.CloseSharedConnection();
            Interlocked.Exchange(ref _isLocked, 1);
        }
    }
}