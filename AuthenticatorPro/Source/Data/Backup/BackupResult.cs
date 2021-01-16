using System;
using Android.Content;

namespace AuthenticatorPro.Data.Backup
{
    internal class BackupResult : IResult
    {
        public readonly string FileName;

        public BackupResult(string fileName = null)
        {
            FileName = fileName;
        }
        
        public bool IsVoid()
        {
            return String.IsNullOrEmpty(FileName);
        }

        public string ToString(Context context)
        {
            return IsVoid()
                ? context.GetString(Resource.String.noAuthenticators)
                : String.Format(context.GetString(Resource.String.backupSuccess), FileName);
        }
    }
}