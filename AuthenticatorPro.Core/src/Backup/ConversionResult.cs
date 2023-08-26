using System.Collections.Generic;

namespace AuthenticatorPro.Core.Backup
{
    public class ConversionResult
    {
        public ConversionResult()
        {
            Failures = new List<ConversionFailure>();
        }

        public Backup Backup { get; set; }
        public List<ConversionFailure> Failures { get; set; }
    }
}