using System;
using Android.Content;

namespace AuthenticatorPro.Droid.Data.Backup
{
    internal class BackupResult : IResult
    {
        private readonly string _fileName;

        public BackupResult(string fileName = null)
        {
            _fileName = fileName;
        }
        
        public bool IsVoid()
        {
            return String.IsNullOrEmpty(_fileName);
        }

        public string ToString(Context context)
        {
            return IsVoid()
                ? context.GetString(Resource.String.noAuthenticators)
                : String.Format(context.GetString(Resource.String.backupSuccess), _fileName);
        }
    }
}