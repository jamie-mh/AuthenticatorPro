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
        
        public AutoBackupWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            _context = context;
        }

        private enum NotificationContext
        {
            Failure, TestRunSuccess
        }

        private async Task DoBackup()
        {
            var connection = await Database.Connect(_context);
            
            var categorySource = new CategorySource(connection);
            var customIconSource = new CustomIconSource(connection);
            var authSource = new AuthenticatorSource(connection);

            await categorySource.Update();
            await customIconSource.Update();
            await authSource.Update();

            if(!authSource.GetAll().Any())
                return;

            var prefs = PreferenceManager.GetDefaultSharedPreferences(_context);
            var backupDirUriStr = prefs.GetString("pref_autoBackupUri", null);

            if(backupDirUriStr == null)
                throw new Exception("No backup URI defined.");

            var password = await SecureStorage.GetAsync("autoBackupPassword");

            if(password == null)
                throw new Exception("No password defined.");
            
            var backup = new Backup(
                authSource.GetAll(),
                categorySource.GetAll(),
                authSource.CategoryBindings,
                customIconSource.GetAll()
            );

            var dataToWrite = backup.ToBytes(password);

            var directory = DocumentFile.FromTreeUri(_context, Uri.Parse(backupDirUriStr));
            var file = directory.CreateFile("application/octet-stream", $"backup-{DateTime.Now:yyyy-MM-dd}.authpro");
            
            if(file == null)
                throw new Exception("File creation failed, got null.");

            await FileUtil.WriteFile(_context, file.Uri, dataToWrite);
            await connection.CloseAsync();
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

        private void ShowNotification(NotificationContext context)
        {
            var builder = new NotificationCompat.Builder(_context, NotificationChannelId)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetLargeIcon(BitmapFactory.DecodeResource(_context.Resources, Resource.Mipmap.ic_launcher))
                .SetPriority(NotificationCompat.PriorityLow);

            switch(context)
            {
                case NotificationContext.Failure:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupFailureTitle));
                    builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(_context.GetString(Resource.String.autoBackupFailureText)));
                    
                    var intent = new Intent(_context, typeof(MainActivity));
                    intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, 0);
                    builder.SetContentIntent(pendingIntent);
                    builder.SetAutoCancel(true);
                    break;
                
                case NotificationContext.TestRunSuccess:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupTestTitle));
                    builder.SetContentText(_context.GetString(Resource.String.autoBackupTestText));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }

            CreateNotificationChannel();
            var manager = NotificationManagerCompat.From(_context);
            manager.Notify(NotificationId, builder.Build());
        }
        
        public override Result DoWork()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(_context);
            var needsBackup = prefs.GetBoolean("needsBackup", false);
            var enabled = prefs.GetBoolean("pref_autoBackupEnabled", false);
            
            var isTestRun = prefs.GetBoolean("autoBackupTestRun", false);
            prefs.Edit().PutBoolean("autoBackupTestRun", false).Commit();
            
            if(!isTestRun && (!needsBackup || !enabled))
                return Result.InvokeSuccess();
            
            try
            {
                DoBackup().GetAwaiter().GetResult();
                prefs.Edit().PutBoolean("needsBackup", false).Commit();
            }
            catch(Exception e)
            {
                ShowNotification(NotificationContext.Failure);
                Log.Error("AUTHPRO", e.ToString());
                return Result.InvokeFailure();
            }
            
            if(isTestRun)
                ShowNotification(NotificationContext.TestRunSuccess);

            return Result.InvokeSuccess();
        }
    }
}