using Android.Content;
using AuthenticatorPro.Core.Backup;
using System;

namespace AuthenticatorPro.Droid.Extension
{
    internal static class ResultExt
    {
        public static string ToString(this RestoreResult result, Context context)
        {
            if (result.IsVoid())
            {
                return context.GetString(Resource.String.restoredNothing);
            }

            return result.UpdatedAuthenticatorCount > 0
                ? String.Format(context.GetString(Resource.String.restoredFromBackupUpdated),
                    result.AddedAuthenticatorCount,
                    result.AddedCategoryCount, result.UpdatedAuthenticatorCount)
                : String.Format(context.GetString(Resource.String.restoredFromBackup), result.AddedAuthenticatorCount,
                    result.AddedCategoryCount);
        }

        public static string ToString(this BackupResult result, Context context)
        {
            return result.IsVoid()
                ? context.GetString(Resource.String.noAuthenticators)
                : String.Format(context.GetString(Resource.String.backupSuccess), result.FileName);
        }
    }
}