using System;
using AndroidX.Activity;

namespace Stratum.Droid.Callback
{
    public class BackPressCallback : OnBackPressedCallback
    {
        private readonly bool _enabled;

        public BackPressCallback(bool enabled) : base(enabled)
        {
            _enabled = enabled;
        }

        public event EventHandler BackPressed;

        public override void HandleOnBackPressed()
        {
            if (_enabled)
            {
                BackPressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}