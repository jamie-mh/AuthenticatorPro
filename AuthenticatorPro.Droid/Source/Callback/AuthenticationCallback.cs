// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Hardware.Biometrics;
using Java.Lang;
using BiometricPrompt = AndroidX.Biometric.BiometricPrompt;

namespace AuthenticatorPro.Droid.Callback
{
    internal class AuthenticationCallback : BiometricPrompt.AuthenticationCallback
    {
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler Failed;
        public event EventHandler<BiometricPrompt.AuthenticationResult> Success;
        
        public override void OnAuthenticationError(int errorCode, ICharSequence errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            var args = new ErrorEventArgs((BiometricErrorCode) errorCode, errString.ToString());
            Error?.Invoke(this, args);
        }

        public override void OnAuthenticationFailed()
        {
            base.OnAuthenticationFailed();
            Failed?.Invoke(this, null);
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            Success?.Invoke(this, result);
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