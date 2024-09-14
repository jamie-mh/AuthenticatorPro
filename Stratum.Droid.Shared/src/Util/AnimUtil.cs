// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Views;
using Android.Views.Animations;

namespace Stratum.Droid.Shared.Util
{
    public static class AnimUtil
    {
        public const int LengthShort = 200;
        public const int LengthLong = 500;

        public static void FadeInView(View view, int length, bool overrideAnim = false,
            Action callback = null)
        {
            if (overrideAnim)
            {
                view.Animation?.Cancel();
                view.ClearAnimation();
                view.Visibility = ViewStates.Invisible;
            }
            else if (view.Visibility != ViewStates.Invisible)
            {
                callback?.Invoke();
                return;
            }

            var anim = new AlphaAnimation(0f, 1f) { Duration = length };

            anim.AnimationEnd += delegate
            {
                view.Visibility = ViewStates.Visible;
                callback?.Invoke();
            };

            view.StartAnimation(anim);
        }

        public static void FadeOutView(View view, int length, bool overrideAnim = false,
            Action callback = null)
        {
            if (overrideAnim)
            {
                view.Animation?.Cancel();
                view.ClearAnimation();
                view.Visibility = ViewStates.Visible;
            }
            else if (view.Visibility != ViewStates.Visible)
            {
                callback?.Invoke();
                return;
            }

            var anim = new AlphaAnimation(1f, 0f) { Duration = length };

            anim.AnimationEnd += delegate
            {
                view.Visibility = ViewStates.Invisible;
                callback?.Invoke();
            };

            view.StartAnimation(anim);
        }
    }
}