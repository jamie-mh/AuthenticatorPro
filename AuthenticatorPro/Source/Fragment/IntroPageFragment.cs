using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class IntroPageFragment : AndroidX.Fragment.App.Fragment
    {
        private readonly int _position;

        public IntroPageFragment(int position)
        {
            RetainInstance = true;
            _position = position;            
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragmentIntroPage, container, false);

            var message = view.FindViewById<TextView>(Resource.Id.textMessage);
            var title = view.FindViewById<TextView>(Resource.Id.textTitle);
            var image = view.FindViewById<ImageView>(Resource.Id.image);

            title.Text = Resources.GetStringArray(Resource.Array.introTitle)[_position];
            message.Text = Resources.GetStringArray(Resource.Array.introText)[_position];

            var imageArray = Resources.ObtainTypedArray(Resource.Array.introImage);
            image.SetImageResource(imageArray.GetResourceId(_position, -1));

            return view;
        }
    }
}