// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.DocumentFile.Provider;
using AndroidX.Work;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Backup.Encryption;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Extension;
using AuthenticatorPro.Droid.Util;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid
{
    public class AutoBackupWorker : Worker
    {
        public const string Name = "autobackup";

        private readonly Context _context;
        private readonly PreferenceWrapper _preferences;
        private readonly SecureStorageWrapper _secureStorageWrapper;
        private readonly Database _database;
        private readonly IBackupService _backupService;

        public AutoBackupWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            _context = context;
            _preferences = new PreferenceWrapper(context);
            _secureStorageWrapper = new SecureStorageWrapper(context);
            _database = new Database();

            using var container = Dependencies.GetChildContainer();
            container.Register(_database);
            Dependencies.RegisterRepositories(container);
            Dependencies.RegisterServices(container);

            _backupService = container.Resolve<IBackupService>();
        }

        private async Task OpenDatabase()
        {
            var password = _secureStorageWrapper.GetDatabasePassword();
            await _database.OpenAsync(password, Database.Origin.AutoBackup);
        }

        private async Task CloseDatabase()
        {
            await _database.CloseAsync(Database.Origin.AutoBackup);
        }

        private bool HasPersistentPermissionsAtUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri), "No uri provided");
            }

            // Uris cannot be compared directly for some reason, use string representation
            var permission =
                _context.ContentResolver.PersistedUriPermissions.FirstOrDefault(p =>
                    p.Uri.ToString() == uri.ToString());

            if (permission == null)
            {
                return false;
            }

            return permission.IsReadPermission && permission.IsWritePermission;
        }

        private string GetBackupPassword()
        {
            return _preferences.DatabasePasswordBackup
                ? _secureStorageWrapper.GetDatabasePassword()
                : _secureStorageWrapper.GetAutoBackupPassword();
        }

        private async Task<BackupResult> BackupToDir(Uri destUri)
        {
            var backup = await _backupService.CreateBackupAsync();

            if (!backup.Authenticators.Any())
            {
                return new BackupResult();
            }

            if (!HasPersistentPermissionsAtUri(destUri))
            {
                throw new InvalidOperationException("No permission at URI");
            }

            var password = GetBackupPassword();

            if (password == null)
            {
                throw new InvalidOperationException("No password defined");
            }

            IBackupEncryption encryption = !string.IsNullOrEmpty(password)
                ? new StrongBackupEncryption()
                : new NoBackupEncryption();

            var dataToWrite = await encryption.EncryptAsync(backup, password);

            var directory = DocumentFile.FromTreeUri(_context, destUri);
            var file = directory.CreateFile(Backup.MimeType,
                FormattableString.Invariant($"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.{Backup.FileExtension}"));

            if (file == null)
            {
                throw new InvalidOperationException("File creation failed, got null");
            }

            await FileUtil.WriteFile(_context, file.Uri, dataToWrite);
            return new BackupResult(file.Name);
        }

        private void CreateNotificationChannel(NotificationContext context)
        {
            var idString = ((int) context).ToString();

            var name = context switch
            {
                NotificationContext.BackupFailure => _context.GetString(Resource.String.autoBackupFailureTitle),
                NotificationContext.BackupSuccess => _context.GetString(Resource.String.autoBackupSuccessTitle),
                _ => throw new ArgumentOutOfRangeException(nameof(context))
            };

#pragma warning disable CA1416
            var channel = new NotificationChannel(idString, name, NotificationImportance.Low);
            var manager = NotificationManagerCompat.From(_context);
            manager.CreateNotificationChannel(channel);
#pragma warning restore CA1416
        }

        private void ShowNotification(NotificationContext context, bool openAppOnClick, IResult result = null)
        {
            var channelId = ((int) context).ToString();

            var builder = new NotificationCompat.Builder(_context, channelId)
                .SetSmallIcon(Resource.Drawable.ic_notification)
                .SetColor(ContextCompat.GetColor(_context, Shared.Resource.Color.colorLightBluePrimary))
                .SetPriority(NotificationCompat.PriorityLow);

            switch (context)
            {
                case NotificationContext.BackupFailure:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupFailureTitle));
                    builder.SetStyle(
                        new NotificationCompat.BigTextStyle().BigText(
                            _context.GetString(Resource.String.autoBackupFailureText)));
                    break;

                case NotificationContext.BackupSuccess:
                {
                    var backupResult = (BackupResult) result;
                    builder.SetContentTitle(_context.GetString(Resource.String.autoBackupSuccessTitle));
                    builder.SetContentText(backupResult.ToString(_context));
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }

            if (openAppOnClick)
            {
                var intent = new Intent(_context, typeof(MainActivity));
                intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, PendingIntentFlags.Immutable);
                builder.SetContentIntent(pendingIntent);
                builder.SetAutoCancel(true);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                CreateNotificationChannel(context);
            }

            var manager = NotificationManagerCompat.From(_context);
            manager.Notify((int) context, builder.Build());
        }

        private async Task<Result> DoWorkAsync()
        {
            var destination = _preferences.AutoBackupUri;

            var backupTriggered = _preferences.AutoBackupTrigger;
            _preferences.AutoBackupTrigger = false;

            if (backupTriggered ||
                (_preferences.AutoBackupEnabled && _preferences.BackupRequired != BackupRequirement.NotRequired))
            {
                try
                {
                    await OpenDatabase();
                    var result = await BackupToDir(destination);
                    ShowNotification(NotificationContext.BackupSuccess, false, result);
                    _preferences.BackupRequired = BackupRequirement.NotRequired;
                }
                catch (Exception e)
                {
                    ShowNotification(NotificationContext.BackupFailure, true);
                    Logger.Error(e);
                    return Result.InvokeFailure();
                }
                finally
                {
                    await CloseDatabase();
                }
            }

            return Result.InvokeSuccess();
        }

        public override Result DoWork()
        {
            return DoWorkAsync().GetAwaiter().GetResult();
        }

        private enum NotificationContext
        {
            BackupFailure,
            BackupSuccess
        }
    }
}