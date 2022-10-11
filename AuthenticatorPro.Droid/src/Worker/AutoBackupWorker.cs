// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.DocumentFile.Provider;
using AndroidX.Work;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Data.Backup;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Service;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Logger = AuthenticatorPro.Droid.Util.Logger;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Worker
{
    internal class AutoBackupWorker : AndroidX.Work.Worker
    {
        public const string Name = "autobackup";

        private readonly Context _context;
        private readonly PreferenceWrapper _preferences;
        private readonly Database _database;

        private readonly IRestoreService _restoreService;

        private readonly IAuthenticatorRepository _authenticatorRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly ICustomIconRepository _customIconRepository;


        private enum NotificationContext
        {
            BackupFailure, RestoreFailure, RestoreSuccess, BackupSuccess
        }

        public AutoBackupWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            _context = context;
            _preferences = new PreferenceWrapper(context);

            _database = Dependencies.Resolve<Database>("autoBackupDatabase");
            _restoreService = Dependencies.Resolve<IRestoreService>();
            _authenticatorRepository = Dependencies.Resolve<IAuthenticatorRepository>();
            _categoryRepository = Dependencies.Resolve<ICategoryRepository>();
            _authenticatorCategoryRepository = Dependencies.Resolve<IAuthenticatorCategoryRepository>();
            _customIconRepository = Dependencies.Resolve<ICustomIconRepository>();
        }

        private async Task OpenDatabase()
        {
            var password = await SecureStorageWrapper.GetDatabasePassword();
            await _database.Open(password, Database.Origin.AutoBackup);
        }

        private async Task CloseDatabase()
        {
            await _database.Close(Database.Origin.AutoBackup);
        }

        private bool HasPersistablePermissionsAtUri(Uri uri)
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

        private async Task<string> GetBackupPassword()
        {
            if (_preferences.DatabasePasswordBackup)
            {
                return await SecureStorageWrapper.GetDatabasePassword();
            }

            return await SecureStorageWrapper.GetAutoBackupPassword();
        }

        private async Task<BackupResult> BackupToDir(Uri destUri)
        {
            var auths = (await _authenticatorRepository.GetAllAsync()).ToImmutableArray();

            if (!auths.Any())
            {
                return new BackupResult();
            }

            if (!HasPersistablePermissionsAtUri(destUri))
            {
                throw new InvalidOperationException("No permission at URI");
            }

            var password = await GetBackupPassword();

            if (password == null)
            {
                throw new InvalidOperationException("No password defined");
            }

            var backup = new Backup(
                auths,
                await _categoryRepository.GetAllAsync(),
                await _authenticatorCategoryRepository.GetAllAsync(),
                await _customIconRepository.GetAllAsync()
            );

            var dataToWrite = backup.ToBytes(password);

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

        private async Task<RestoreResult> RestoreFromDir(Uri destUri)
        {
            if (!HasPersistablePermissionsAtUri(destUri))
            {
                throw new InvalidOperationException("No permission at URI");
            }

            var directory = DocumentFile.FromTreeUri(_context, destUri);
            var files = directory.ListFiles();

            var mostRecentBackup = files
                .Where(f => f.IsFile && f.Type == Backup.MimeType && f.Name.EndsWith(Backup.FileExtension) &&
                            f.Length() > 0 && f.CanRead())
                .OrderByDescending(f => f.LastModified())
                .FirstOrDefault();

            if (mostRecentBackup == null || mostRecentBackup.LastModified() <= _preferences.MostRecentBackupModifiedAt)
            {
                return new RestoreResult();
            }

            _preferences.MostRecentBackupModifiedAt = mostRecentBackup.LastModified();
            var password = await GetBackupPassword();

            if (password == null)
            {
                throw new InvalidOperationException("No password defined.");
            }

            var data = await FileUtil.ReadFile(_context, mostRecentBackup.Uri);
            var backup = Backup.FromBytes(data, password);
            return await _restoreService.RestoreAndUpdateAsync(backup);
        }

        private void CreateNotificationChannel(NotificationContext context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var idString = ((int) context).ToString();

            var name = context switch
            {
                NotificationContext.BackupFailure => _context.GetString(Resource.String.autoBackupFailureTitle),
                NotificationContext.RestoreFailure => _context.GetString(Resource.String.autoRestoreFailureTitle),
                NotificationContext.RestoreSuccess => _context.GetString(Resource.String.autoRestoreSuccessTitle),
                NotificationContext.BackupSuccess => _context.GetString(Resource.String.autoBackupSuccessTitle),
                _ => throw new ArgumentOutOfRangeException(nameof(context))
            };

            var channel = new NotificationChannel(idString, name, NotificationImportance.Low);
            var manager = NotificationManagerCompat.From(_context);
            manager.CreateNotificationChannel(channel);
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

                case NotificationContext.RestoreFailure:
                    builder.SetContentTitle(_context.GetString(Resource.String.autoRestoreFailureTitle));
                    builder.SetStyle(
                        new NotificationCompat.BigTextStyle().BigText(
                            _context.GetString(Resource.String.autoRestoreFailureText)));
                    break;

                case NotificationContext.RestoreSuccess:
                {
                    var restoreResult = (RestoreResult) result;
                    builder.SetContentTitle(_context.GetString(Resource.String.autoRestoreSuccessTitle));
                    builder.SetContentText(restoreResult.ToString(_context));
                    break;
                }

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

            if (!backupTriggered && (restoreTriggered || _preferences.AutoRestoreEnabled))
            {
                try
                {
                    await OpenDatabase();
                    var result = await RestoreFromDir(destination);

                    if (!result.IsVoid() || restoreTriggered)
                    {
                        ShowNotification(NotificationContext.RestoreSuccess, true, result);
                    }

                    if (!result.IsVoid())
                    {
                        _preferences.AutoRestoreCompleted = true;
                    }
                }
                catch (Exception e)
                {
                    restoreSucceeded = false;
                    ShowNotification(NotificationContext.RestoreFailure, true);
                    Logger.Error(e);
                }
            }

            var backupSucceeded = true;

            if (!restoreTriggered && (backupTriggered || (_preferences.AutoBackupEnabled && restoreSucceeded &&
                                                          _preferences.BackupRequired !=
                                                          BackupRequirement.NotRequired)))
            {
                try
                {
                    await OpenDatabase();
                    var result = await BackupToDir(destination);
                    ShowNotification(NotificationContext.BackupSuccess, false, result);
                    _preferences.BackupRequired = BackupRequirement.NotRequired;

                    // Don't update value if backup triggered, won't combine with restore
                    if (!result.IsVoid() && !backupTriggered)
                    {
                        _preferences.MostRecentBackupModifiedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    }
                }
                catch (Exception e)
                {
                    backupSucceeded = false;
                    ShowNotification(NotificationContext.BackupFailure, true);
                    Logger.Error(e);
                }
            }

            await CloseDatabase();

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