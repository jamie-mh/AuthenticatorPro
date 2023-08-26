// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.TextView;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    public class IntroPageFragment : AndroidX.Fragment.App.Fragment
    {
        private int _position;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _position = Arguments.GetInt("position", -1);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragmentIntroPage, container, false);

            var summary = view.FindViewById<MaterialTextView>(Resource.Id.textSummary);
            var title = view.FindViewById<MaterialTextView>(Resource.Id.textTitle);
            var image = view.FindViewById<ImageView>(Resource.Id.image);

            title.Text = Resources.GetStringArray(Resource.Array.introTitle)[_position];
            summary.Text = Resources.GetStringArray(Resource.Array.introSummary)[_position];

            var imageArray = Resources.ObtainTypedArray(Resource.Array.introImage);
            image.SetImageResource(imageArray.GetResourceId(_position, -1));

            return view;
        }
    }
}