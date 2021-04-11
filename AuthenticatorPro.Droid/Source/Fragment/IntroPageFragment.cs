using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class IntroPageFragment : AndroidX.Fragment.App.Fragment
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

            var summary = view.FindViewById<TextView>(Resource.Id.textSummary);
            var title = view.FindViewById<TextView>(Resource.Id.textTitle);
            var image = view.FindViewById<ImageView>(Resource.Id.image);

            title.Text = Resources.GetStringArray(Resource.Array.introTitle)[_position];
            summary.Text = Resources.GetStringArray(Resource.Array.introSummary)[_position];

            var imageArray = Resources.ObtainTypedArray(Resource.Array.introImage);
            image.SetImageResource(imageArray.GetResourceId(_position, -1));

            return view;
        }
    }
}