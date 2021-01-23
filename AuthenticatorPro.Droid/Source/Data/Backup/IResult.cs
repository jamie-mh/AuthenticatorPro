using Android.Content;

namespace AuthenticatorPro.Droid.Data.Backup
{
    internal interface IResult
    {
        public bool IsVoid();
        public string ToString(Context context);
    }
}