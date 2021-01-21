using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AuthenticatorPro.Data.Backup.Converter
{
    internal class UriListBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<Backup> Convert(byte[] data, string password = null)
        {
            var text = Encoding.UTF8.GetString(data);
            var lines = text.Split(new[] {"\r\n"}, StringSplitOptions.None);

            var authenticators = new List<Authenticator>();
            authenticators.AddRange(lines.Where(line => !String.IsNullOrWhiteSpace(line)).Select(Authenticator.FromOtpAuthUri));

            return Task.FromResult(new Backup(authenticators));
        }
    }
}