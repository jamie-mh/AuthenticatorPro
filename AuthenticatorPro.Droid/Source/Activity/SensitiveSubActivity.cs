namespace AuthenticatorPro.Droid.Activity
{
    internal class SensitiveSubActivity : BaseActivity
    {
        protected override void OnResume()
        {
            base.OnResume();
            
            if(BaseApplication.IsLocked)
                Finish();
        }
    }
}