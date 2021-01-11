using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AuthenticatorPro.Data.Backup.Converter
{
    internal class WinAuthBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<Backup> Convert(byte[] data, string password = null)
        {
            var text = Encoding.UTF8.GetString(data);
            var lines = text.Split(new[] {"\r\n"}, StringSplitOptions.None);

            var authenticators = new List<Authenticator>();
            authenticators.AddRange(from line in lines where !String.IsNullOrWhiteSpace(line) select Authenticator.FromOtpAuthUri(line));

            return Task.FromResult(new Backup(authenticators, null, null, null));
        }
    }
}