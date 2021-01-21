using System;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Runtime;
using AndroidX.Lifecycle;
using AuthenticatorPro.Data;
using AuthenticatorPro.Util;
using Java.Interop;

namespace AuthenticatorPro
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
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
            _preferences = new PreferenceWrapper(Context);
            IsLocked = true;
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

        public async Task Unlock(string password)
        {
            IsLocked = false;
            await Database.OpenSharedConnection(password);
        }

        private async Task Lock()
        {
            IsLocked = true;
            await Database.CloseSharedConnection();
        }
    }
}