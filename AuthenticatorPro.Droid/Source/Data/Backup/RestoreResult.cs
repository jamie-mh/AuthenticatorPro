using System;
using Android.Content;

namespace AuthenticatorPro.Droid.Data.Backup
{
    internal class RestoreResult : IResult
    {
        public readonly int AddedAuthenticatorCount;
        public readonly int UpdatedAuthenticatorCount;
        public readonly int CategoryCount;
        public readonly int CustomIconCount;

        public RestoreResult(int addedAuthenticatorCount = 0, int updatedAuthenticatorCount = 0, int categoryCount = 0, int customIconCount = 0)
        {
            AddedAuthenticatorCount = addedAuthenticatorCount;
            UpdatedAuthenticatorCount = updatedAuthenticatorCount;
            CategoryCount = categoryCount;
            CustomIconCount = customIconCount;
        }

        public bool IsVoid()
        {
            return AddedAuthenticatorCount == 0 && UpdatedAuthenticatorCount == 0 && CategoryCount == 0 && CustomIconCount == 0;
        }

        public string ToString(Context context)
        {
            if(IsVoid())
                return context.GetString(Resource.String.restoredNothing);
            
            return UpdatedAuthenticatorCount > 0
                ? String.Format(context.GetString(Resource.String.restoredFromBackupUpdated), AddedAuthenticatorCount, CategoryCount, UpdatedAuthenticatorCount)
                : String.Format(context.GetString(Resource.String.restoredFromBackup), AddedAuthenticatorCount, CategoryCount);
        }
    }
}