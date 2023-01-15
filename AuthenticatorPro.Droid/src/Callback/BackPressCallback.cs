using AndroidX.Activity;
using System;

namespace AuthenticatorPro.Droid.Callback
{
    public class BackPressCallback : OnBackPressedCallback
    {
        public event EventHandler BackPressed;
        private readonly bool _enabled;

        public BackPressCallback(bool enabled) : base(enabled)
        {
            _enabled = enabled;
        }

        public override void HandleOnBackPressed()
        {
            if (_enabled)
            {
                BackPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}