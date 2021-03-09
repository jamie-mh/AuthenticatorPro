using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using AuthenticatorPro.Shared.Source.Data;
using QRCoder;

namespace AuthenticatorPro.Droid.Data.Backup
{
    internal class UriListBackup 
    {
        public const string FileExtension = "txt";
        public const string MimeType = "text/plain";
        
        private readonly string _contents;

        private UriListBackup(string contents)
        {
            _contents = contents;
        }

        public override string ToString()
        {
            return _contents;
        }

        public static UriListBackup FromAuthenticators(IEnumerable<Authenticator> authenticators)
        {
            var builder = new StringBuilder();

            foreach(var auth in authenticators)
            {
                string uri;

                try
                {
                    uri = auth.GetOtpAuthUri();
                }
                catch(NotSupportedException)
                {
                    continue;
                }

                builder.AppendLine(uri);
            }

            return new UriListBackup(builder.ToString());
        }
    }
}