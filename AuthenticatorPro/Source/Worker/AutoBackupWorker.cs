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
using AndroidX.Preference;
using AndroidX.Work;
using AuthenticatorPro.Activity;
using AuthenticatorPro.Data;
using AuthenticatorPro.Data.Backup;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.Util;
using SQLite;
using Xamarin.Essentials;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Worker
{
    internal class AutoBackupWorker : AndroidX.Work.Worker
    {
        public const string Name = "autobackup";
        private const int NotificationId = 0;
        
        private readonly Context _context;
        private readonly Lazy<Task> _initTask;
        
        private SQLiteAsyncConnection _connection;
        private AuthenticatorSource _authSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;
        
        
        public AutoBackupWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            _context = context;
            
            _initTask = new Lazy<Task>(async delegate
            {
                _connection = await Database.Connect(ApplicationContext);
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

            var password = await SecureStorage.GetAsync("autoBackupPassword");

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
            var file = directory.CreateFile("application/octet-stream", $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.{Backup.FileExtension}");
            
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
                .Where(f => f.IsFile && f.Type == "application/octet-stream" && f.Name.EndsWith(Backup.FileExtension) && f.Length() > 0 && f.CanRead())
                .OrderByDescending(f => f.LastModified())
                .FirstOrDefault();

            var prefs = PreferenceManager.GetDefaultSharedPreferences(_context);
            var mostRecentBackupModifiedAt = prefs.GetLong("mostRecentBackupModifiedAt", 0);

            if(mostRecentBackup == null || mostRecentBackup.LastModified() <= mostRecentBackupModifiedAt)
                return new RestoreResult();
            
            prefs.Edit().PutLong("mostRecentBackupModifiedAt", mostRecentBackup.LastModified()).Commit();
            var password = await SecureStorage.GetAsync("autoBackupPassword");

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
                
            return new RestoreResult(authsAdded, authsUpdated, categoriesAdded, customIconsAdded);
        }

        private void CreateNotificationChannel(NotificationContext context)
        {
            if(Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var idString = ((int) context).ToString();
            string name;
            NotificationImportance importance;

            switch(context)
            {
                case NotificationContext.BackupFailure:
                    name = _context.GetString(Resource.String.autoBackupFailureTitle);
                    importance = NotificationImportance.High;
                    break;
                
                case NotificationContext.RestoreFailure:
                    name = _context.GetString(Resource.String.autoRestoreFailureTitle);
                    importance = NotificationImportance.High;
                    break;
                    
                case NotificationContext.RestoreSuccess:
                    name = _context.GetString(Resource.String.autoRestoreSuccessTitle);
                    importance = NotificationImportance.Low;
                    break;

                case NotificationContext.BackupSuccess:
                    name = _context.GetString(Resource.String.autoBackupSuccessTitle);
                    importance = NotificationImportance.Low;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }

            var channel = new NotificationChannel(idString, name, importance);
            var manager = NotificationManagerCompat.From(_context);
            manager.CreateNotificationChannel(channel);
        }

        private string GetUniqueNotificationTag()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        private void ShowNotification(NotificationContext context, bool openAppOnClick, IResult result = null)
        {
            var channelId = ((int) context).ToString();
            
            var builder = new NotificationCompat.Builder(_context, channelId)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetLargeIcon(BitmapFactory.DecodeResource(_context.Resources, Resource.Mipmap.ic_launcher))
                .SetPriority(NotificationCompat.PriorityDefault);

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
            manager.Notify(GetUniqueNotificationTag(), NotificationId, builder.Build());
        }

        private async Task<Result> DoWorkAsync()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(_context);
            
            var destUriStr = prefs.GetString("pref_autoBackupUri", null);
            var destUri = destUriStr != null ? Uri.Parse(destUriStr) : null;
            
            var restoreTriggered = prefs.GetBoolean("autoRestoreTrigger", false);
            
            if(restoreTriggered)
                prefs.Edit().PutBoolean("autoRestoreTrigger", false).Commit();
            
            var backupTriggered = prefs.GetBoolean("autoBackupTrigger", false);
            
            if(backupTriggered)
                prefs.Edit().PutBoolean("autoBackupTrigger", false).Commit();

            var autoRestoreEnabled = prefs.GetBoolean("pref_autoBackupEnabled", false);
            var restoreSucceeded = true;

            if(!backupTriggered && (restoreTriggered || autoRestoreEnabled))
            {
                await _initTask.Value;
                
                try
                {
                    var result = await RestoreFromDir(destUri);
                    
                    if(!result.IsVoid() || restoreTriggered)
                        ShowNotification(NotificationContext.RestoreSuccess, true, result);
                    
                    if(!result.IsVoid())
                        prefs.Edit().PutBoolean("autoRestoreCompleted", true).Commit();
                }
                catch(Exception e)
                {
                    restoreSucceeded = false;
                    ShowNotification(NotificationContext.RestoreFailure, true);
                    Log.Error("AUTHPRO", e.ToString());
                }
            }
            
            var autoBackupEnabled = prefs.GetBoolean("pref_autoBackupEnabled", false);
            var requirement = (BackupRequirement) prefs.GetInt("backupRequirement", (int) BackupRequirement.NotRequired);
            
            var backupSucceeded = true;

            if(!restoreTriggered && (backupTriggered || autoBackupEnabled && restoreSucceeded && requirement != BackupRequirement.NotRequired))
            {
                await _initTask.Value;
                
                try
                {
                    var result = await BackupToDir(destUri);
                    ShowNotification(NotificationContext.BackupSuccess, false, result);
                    
                    var editor = prefs.Edit();
                    editor.PutInt("backupRequirement", (int) BackupRequirement.NotRequired).Commit();

                    // Don't update value if backup triggered, won't combine with restore
                    if(!result.IsVoid() && !backupTriggered)
                        editor.PutLong("mostRecentBackupModifiedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                    editor.Commit();
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