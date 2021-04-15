using System;
using Android.Content;

namespace AuthenticatorPro.Droid.Data.Backup
{
    internal class RestoreResult : IResult
    {
        private readonly int _addedAuthenticatorCount;
        private readonly int _updatedAuthenticatorCount;
        private readonly int _categoryCount;
        private readonly int _customIconCount;

        public RestoreResult(int addedAuthenticatorCount = 0, int updatedAuthenticatorCount = 0, int categoryCount = 0, int customIconCount = 0)
        {
            _addedAuthenticatorCount = addedAuthenticatorCount;
            _updatedAuthenticatorCount = updatedAuthenticatorCount;
            _categoryCount = categoryCount;
            _customIconCount = customIconCount;
        }

        public bool IsVoid()
        {
            return _addedAuthenticatorCount == 0 && _updatedAuthenticatorCount == 0 && _categoryCount == 0 && _customIconCount == 0;
        }

        public string ToString(Context context)
        {
            if(IsVoid())
                return context.GetString(Resource.String.restoredNothing);
            
            return _updatedAuthenticatorCount > 0
                ? String.Format(context.GetString(Resource.String.restoredFromBackupUpdated), _addedAuthenticatorCount, _categoryCount, _updatedAuthenticatorCount)
                : String.Format(context.GetString(Resource.String.restoredFromBackup), _addedAuthenticatorCount, _categoryCount);
        }
    }
}