using Android.Content;

namespace AuthenticatorPro.Data.Backup
{
    internal interface IResult
    {
        public bool IsVoid();
        public string ToString(Context context);
    }
}