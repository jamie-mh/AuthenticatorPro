using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;

namespace AuthenticatorPro.Source.Fragments
{
    internal class IntroPageFragment : Fragment
    {
        private readonly int _position;

        public IntroPageFragment() { }

        public IntroPageFragment(int position) : base()
        {
            _position = position;            
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.introPageFragment, container, false);

            var text = view.FindViewById<TextView>(Resource.Id.introPageFragment_text);
            var title = view.FindViewById<TextView>(Resource.Id.introPageFragment_title);
            var image = view.FindViewById<ImageView>(Resource.Id.introPageFragment_image);

            title.Text = Resources.GetStringArray(Resource.Array.introTitle)[_position];
            text.Text = Resources.GetStringArray(Resource.Array.introText)[_position];

            var imageArray = Resources.ObtainTypedArray(Resource.Array.introImage);
            image.SetImageResource(imageArray.GetResourceId(_position, -1));

            return view;
        }
    }
}