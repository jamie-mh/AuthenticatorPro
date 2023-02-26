using System.Collections.Generic;

namespace AuthenticatorPro.Shared.Backup
{
    public class ConversionResult
    {
        public Backup Backup { get; set; }
        public List<ConversionFailure> Failures { get; set; }

        public ConversionResult()
        {
            Failures = new List<ConversionFailure>();
        }
    }
}