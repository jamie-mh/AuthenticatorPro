using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.DocumentFile.Provider;
using AndroidX.Work;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Data.Backup;
using AuthenticatorPro.Droid.Data.Source;
using AuthenticatorPro.Droid.Util;
using SQLite;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Worker
{
    internal class AutoBackupWorker : AndroidX.Work.Worker
    {
        public const string Name = "autobackup";
        
        private readonly Context _context;
        private readonly PreferenceWrapper _preferences;
        private readonly Lazy<Task> _initTask;
        
        private SQLiteAsyncConnection _connection;
        private AuthenticatorSource _authSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;
        
        
        public AutoBackupWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            _context = context;
            _preferences = new PreferenceWrapper(context);
            
            _initTask = new Lazy<Task>(async delegate
            {
                var password = await SecureStorageWrapper.GetDatabasePassword();
                _connection = await Database.GetPrivateConnection(password);
                _customIconSource = new CustomIconSource(_connection);
                _categorySource = new CategorySource(_connection);
                _authSource = new AuthenticatorSource(_connection);

                await _categorySource.Update();
                await _customIconSource.Update();
                await _authSource.Update();
            });
        }

        private enum NotificationContext
        {
            BackupFailure, RestoreFailure, RestoreSuccess, BackupSuccess
        }

        private bool HasPersistablePermissionsAtUri(Uri uri)
        {
            if(uri == null)
                throw new ArgumentNullException(nameof(uri), "No uri provided");

            // Uris cannot be compared directly for some reason, use string representation
            var permission = _context.ContentResolver.PersistedUriPermissions.FirstOrDefault(p => p.Uri.ToString() == uri.ToString());

            if(permission == null)
                return false;

            return permission.IsReadPermission && permission.IsWritePermission;
        }

        private async Task<BackupResult> BackupToDir(Uri destUri)
        {
            if(!_authSource.GetAll().Any())
                return new BackupResult();

            if(!HasPersistablePermissionsAtUri(destUri))
                throw new Exception("No permission at URI");

            var password = await SecureStorageWrapper.GetAutoBackupPassword();

            if(password == null)
                throw new Exception("No password defined.");
            
            var backup = new Backup(
                _authSource.GetAll(),
                _categorySource.GetAll(),
                _authSource.CategoryBindings,
                _customIconSource.GetAll()
            );

            var dataToWrite = backup.ToBytes(password);

            var directory = DocumentFile.FromTreeUri(_context, destUri);
            var file = directory.CreateFile(Backup.MimeType, $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.{Backup.FileExtension}");
            
            if(file == null)
                throw new Exception("File creation failed, got null.");

            await FileUtil.WriteFile(_context, file.Uri, dataToWrite);
            return new BackupResult(file.Name);
        }

        private async Task<RestoreResult> RestoreFromDir(Uri destUri)
        {
            if(!HasPersistablePermissionsAtUri(destUri))
                throw new Exception("No permission at URI");
              
            var directory = DocumentFile.FromTreeUri(_context, destUri);
            var files = directory.ListFiles();

            var mostRecentBackup = files
                .Where(f => f.IsFile && f.Type == Backup.MimeType && f.Name.EndsWith(Backup.FileExtension) && f.Length() > 0 && f.CanRead())
                .OrderByDescending(f => f.LastModified())
                .FirstOrDefault();

            if(mostRecentBackup == null || mostRecentBackup.LastModified() <= _preferences.MostRecentBackupModifiedAt)
                return new RestoreResult();
            
            _preferences.MostRecentBackupModifiedAt = mostRecentBackup.LastModified();
            var password = await SecureStorageWrapper.GetAutoBackupPassword();

            if(password == null)
                throw new Exception("No password defined.");

            var data = await FileUtil.ReadFile(_context, mostRecentBackup.Uri);
            var backup = Backup.FromBytes(data, password);

            var (authsAdded, authsUpdated) = await _authSource.AddOrUpdateMany(backup.Authenticators);

            var categoriesAdded = backup.Categories != null
                ? await _categorySource.AddMany(backup.Categories)
                : 0;
            
            if(backup.AuthenticatorCategories != null)
                await _authSource.AddOrUpdateManyCategoryBindings(backup.AuthenticatorCategories);
          
            var customIconsAdded = backup.CustomIcons != null
                ? await _customIconSource.AddMany(backup.CustomIcons)
                : 0;
            
            try
            {
                await _customIconSource.CullUnused();
            }
            catch
            {
                // ignored
            }
                
            return new RestoreResult(authsAdded, authsUpdated, categoriesAdded, customIconsAdded);
        }

        private void CreateNotificationChannel(NotificationContext context)
        {
            if(Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var idString = ((int) context).ToString();
            string name;

            switch(context)
            {
                case NotificationContext.BackupFailure:
                    name = _context.GetString(Resource.String.autoBackupFailureTitle);
                    break;
                
                case NotificationContext.RestoreFailure:
                    name = _context.GetString(Resource.String.autoRestoreFailureTitle);
                    break;
                    
                case NotificationContext.RestoreSuccess:
                    name = _context.GetString(Resource.String.autoRestoreSuccessTitle);
                    break;

                case NotificationContext.BackupSuccess:
                    name = _context.GetString(Resource.String.autoBackupSuccessTitle);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }

            var channel = new NotificationChannel(idString, name, NotificationImportance.Low);
            var manager = NotificationManagerCompat.From(_context);
            manager.CreateNotificationChannel(channel);
        }

        private void ShowNotification(NotificationContext context, bool openAppOnClick, IResult result = null)
        {
            var channelId = ((int) context).ToString();
            
            var builder = new NotificationCompat.Builder(_context, channelId)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetLargeIcon(BitmapFactory.DecodeResource(_context.Resources, Resource.Mipmap.ic_launcher))
                .SetPriority(NotificationCompat.PriorityLow);

            switch(context)
            {
                case NotificationContext.BackupFailure:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupFailureTitle));
                    builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(_context.GetString(Resource.String.autoBackupFailureText)));
                    break;
                
                case NotificationContext.RestoreFailure:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoRestoreFailureTitle));
                    builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(_context.GetString(Resource.String.autoRestoreFailureText)));
                    break;
                    
                case NotificationContext.RestoreSuccess:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoRestoreSuccessTitle));
                    builder.SetContentText(result.ToString(_context));
                    break;

                case NotificationContext.BackupSuccess:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupSuccessTitle));
                    builder.SetContentText(result.ToString(_context));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }

            if(openAppOnClick)
            {
                var intent = new Intent(_context, typeof(MainActivity));
                intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, 0);
                builder.SetContentIntent(pendingIntent);
                builder.SetAutoCancel(true);
            }

            CreateNotificationChannel(context);
            var manager = NotificationManagerCompat.From(_context);
            manager.Notify((int) context, builder.Build());
        }

        private async Task<Result> DoWorkAsync()
        {
            var destination = _preferences.AutoBackupUri;

            var restoreTriggered = _preferences.AutoRestoreTrigger;
            _preferences.AutoRestoreTrigger = false;

            var backupTriggered = _preferences.AutoBackupTrigger;
            _preferences.AutoBackupTrigger = false; 
            
            var restoreSucceeded = true;

            if(!backupTriggered && (restoreTriggered || _preferences.AutoRestoreEnabled))
            {
                await _initTask.Value;
                
                try
                {
                    var result = await RestoreFromDir(destination);
                    
                    if(!result.IsVoid() || restoreTriggered)
                        ShowNotification(NotificationContext.RestoreSuccess, true, result);

                    if(!result.IsVoid())
                        _preferences.AutoRestoreCompleted = true;
                }
                catch(Exception e)
                {
                    restoreSucceeded = false;
                    ShowNotification(NotificationContext.RestoreFailure, true);
                    Log.Error("AUTHPRO", e.ToString());
                }
            }
            
            var backupSucceeded = true;

            if(!restoreTriggered && (backupTriggered || _preferences.AutoBackupEnabled && restoreSucceeded && _preferences.BackupRequired != BackupRequirement.NotRequired))
            {
                await _initTask.Value;
                
                try
                {
                    var result = await BackupToDir(destination);
                    ShowNotification(NotificationContext.BackupSuccess, false, result);
                    _preferences.BackupRequired = BackupRequirement.NotRequired;

                    // Don't update value if backup triggered, won't combine with restore
                    if(!result.IsVoid() && !backupTriggered)
                        _preferences.MostRecentBackupModifiedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
                catch(Exception e)
                {
                    backupSucceeded = false;
                    ShowNotification(NotificationContext.BackupFailure, true);
                    Log.Error("AUTHPRO", e.ToString());
                }
            }

            if(_connection != null)
                await _connection.CloseAsync();
            
            return backupSucceeded && restoreSucceeded
                ? Result.InvokeSuccess()
                : Result.InvokeFailure();
        }
        
        public override Result DoWork()
        {
            return DoWorkAsync().GetAwaiter().GetResult();
        }
    }
}