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
        private const string NotificationChannelId = "0";
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
            BackupFailure, RestoreFailure, RestoreSuccess, BackupTriggerSuccess
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

        private async Task BackupToDir(Uri destUri)
        {
            if(!_authSource.GetAll().Any())
                return;

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
            var file = directory.CreateFile("application/octet-stream", $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.authpro");
            
            if(file == null)
                throw new Exception("File creation failed, got null.");

            await FileUtil.WriteFile(_context, file.Uri, dataToWrite);
        }

        private async Task<RestoreResult> RestoreFromDir(Uri destUri, long changesMadeAt)
        {
            if(!HasPersistablePermissionsAtUri(destUri))
                throw new Exception("No permission at URI");
              
            var directory = DocumentFile.FromTreeUri(_context, destUri);
            var files = directory.ListFiles();

            var mostRecentBackup = files
                .Where(f => f.IsFile && f.Type == "application/octet-stream" && f.Length() > 0 && f.CanRead() && f.LastModified() > changesMadeAt)
                .OrderByDescending(f => f.LastModified())
                .FirstOrDefault();
                
            if(mostRecentBackup == null)
                return null;
                
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

        private void CreateNotificationChannel()
        {
            if(Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var channel = new NotificationChannel(NotificationChannelId, _context.GetString(Resource.String.backupStatusChannelName), NotificationImportance.Low)
            {
                Description = _context.GetString(Resource.String.backupStatusChannelDescription)
            };

            var manager = NotificationManagerCompat.From(_context);
            manager.CreateNotificationChannel(channel);
        }

        private void ShowNotification(NotificationContext context, bool openAppOnClick, IResult result = null)
        {
            var builder = new NotificationCompat.Builder(_context, NotificationChannelId)
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
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupFailureTitle));
                    builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(_context.GetString(Resource.String.autoBackupFailureText)));
                    break;
                    
                case NotificationContext.RestoreSuccess:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoRestoreSuccessTitle));

                    if(result == null || result.IsVoid())
                        builder.SetContentText(_context.GetString(Resource.String.restoredNothing));
                    else
                    {
                        var text = result.ToString(_context);
                        builder.SetContentText(text);
                    }
                    break;
                
                case NotificationContext.BackupTriggerSuccess:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupTestTitle));
                    builder.SetContentText(_context.GetString(Resource.String.autoBackupTestText));
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

            CreateNotificationChannel();
            var manager = NotificationManagerCompat.From(_context);
            manager.Notify(NotificationId, builder.Build());
        }

        private async Task<Result> DoWorkAsync()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(_context);
            
            var changesMadeAt = prefs.GetLong("changesMadeAt", 0);
            
            var destUriStr = prefs.GetString("pref_autoBackupUri", null);
            var destUri = destUriStr != null ? Uri.Parse(destUriStr) : null;
            
            var restoreTriggered = prefs.GetBoolean("autoRestoreTrigger", false);
            
            if(restoreTriggered)
                prefs.Edit().PutBoolean("autoRestoreTrigger", false).Commit();

            var autoRestoreEnabled = prefs.GetBoolean("pref_autoBackupEnabled", false);
            var restoreSucceeded = true;

            if(restoreTriggered || autoRestoreEnabled)
            {
                await _initTask.Value;
                
                try
                {
                    var result = await RestoreFromDir(destUri, changesMadeAt);

                    if(restoreTriggered || result != null && !result.IsVoid())
                    {
                        ShowNotification(NotificationContext.RestoreSuccess, false, result);
                        prefs.Edit().PutBoolean("autoRestoreCompleted", true).Commit();
                    }
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
            
            var backupTriggered = prefs.GetBoolean("autoBackupTrigger", false);
            
            if(backupTriggered)
                prefs.Edit().PutBoolean("autoBackupTrigger", false).Commit();
            
            var backupSucceeded = true;

            if(backupTriggered || autoBackupEnabled && restoreSucceeded && requirement != BackupRequirement.NotRequired)
            {
                await _initTask.Value;
                
                try
                {
                    await BackupToDir(destUri);
                }
                catch(Exception e)
                {
                    backupSucceeded = false;
                    ShowNotification(NotificationContext.BackupFailure, true);
                    Log.Error("AUTHPRO", e.ToString());
                }

                if(backupSucceeded)
                {
                    if(backupTriggered)
                        ShowNotification(NotificationContext.BackupTriggerSuccess, false);
                    
                    prefs.Edit().PutInt("backupRequirement", (int) BackupRequirement.NotRequired).Commit();
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