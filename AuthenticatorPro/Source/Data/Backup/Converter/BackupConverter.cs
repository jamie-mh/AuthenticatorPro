using System.Threading.Tasks;

namespace AuthenticatorPro.Data.Backup.Converter
{
    internal abstract class BackupConverter
    {
        public enum BackupPasswordPolicy
        {
            Never, Always, Maybe
        }
        
        public abstract BackupPasswordPolicy PasswordPolicy { get; }
        public abstract Task<Backup> Convert(byte[] data, string password = null);
    }
}