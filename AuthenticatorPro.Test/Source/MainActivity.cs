using System.Reflection;
using Android.App;
using Android.OS;
using Xamarin.Android.NUnitLite;

namespace AuthenticatorPro.Test
{
    [Activity(Label = "AuthenticatorPro.Test", MainLauncher = true)]
    internal class MainActivity : TestSuiteActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            AddTest(Assembly.GetExecutingAssembly());
            base.OnCreate(bundle);
        }
    }
}