// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Hardware.Biometrics;
using Java.Lang;
using BiometricPrompt = AndroidX.Biometric.BiometricPrompt;

namespace Stratum.Droid.Callback
{
    public class AuthenticationCallback : BiometricPrompt.AuthenticationCallback
    {
        public event EventHandler<ErrorEventArgs> Errored;
        public event EventHandler Failed;
        public event EventHandler<BiometricPrompt.AuthenticationResult> Succeeded;

        public override void OnAuthenticationError(int errorCode, ICharSequence errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            var args = new ErrorEventArgs((BiometricErrorCode) errorCode, errString.ToString());
            Errored?.Invoke(this, args);
        }

        public override void OnAuthenticationFailed()
        {
            base.OnAuthenticationFailed();
            Failed?.Invoke(this, EventArgs.Empty);
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            Succeeded?.Invoke(this, result);
        }

        public class ErrorEventArgs : EventArgs
        {
            public readonly BiometricErrorCode Code;
            public readonly string Message;

            public ErrorEventArgs(BiometricErrorCode code, string message)
            {
                Code = code;
                Message = message;
            }
        }
    }
}