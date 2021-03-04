using System;
using AndroidX.Activity.Result;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.Callback
{
    internal class ActivityResultCallback : Object, IActivityResultCallback
    {
        public event EventHandler<ActivityResult> Result; 
        
        public void OnActivityResult(Object obj)
        {
            var result = (ActivityResult) obj;
            Result?.Invoke(this, result);
        }
    }
}