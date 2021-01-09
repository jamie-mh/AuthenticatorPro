using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using AuthenticatorPro.Shared.Data;
using QRCoder;

namespace AuthenticatorPro.Data.Backup
{
    internal class HtmlBackup
    {
        private const string BackupTemplateFileName = "backup_template.html";
        private const int PixelsPerModule = 4;
        
        private readonly string _contents;

        private HtmlBackup(string contents)
        {
            _contents = contents;
        }

        public override string ToString()
        {
            return _contents;
        }

        private static async Task<string> GetTemplate(Context context)
        {
            Stream stream = null;
            StreamReader reader = null;

            try
            {
                stream = context.Assets.Open(BackupTemplateFileName);
                reader = new StreamReader(stream);
                
                return await reader.ReadToEndAsync();
            }
            finally
            {
                stream?.Close();
                reader?.Close();
            }
        }

        public static async Task<HtmlBackup> FromAuthenticatorList(Context context, List<Authenticator> authenticators)
        {
            var template = await GetTemplate(context);
            
            var itemsHtml = new StringBuilder();
            var generator = new QRCodeGenerator();
            
            async Task<string> GetQrCodeDataAsync(string uri)
            {
                return await Task.Run(() =>
                {
                    var qrCodeData = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new PngByteQRCode(qrCodeData);

                    var bytes = qrCode.GetGraphic(PixelsPerModule);
                    return Convert.ToBase64String(bytes);
                });
            }

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
                
                var qrCode = await GetQrCodeDataAsync(uri);

                itemsHtml.Append($@"
                    <tr>
                        <td>{auth.Issuer}</td>
                        <td>{auth.Username}</td>
                        <td><code>{uri}</code></td>
                        <td><img src=""data:image/png;base64,{qrCode}""></td>
                    </tr>
                ");
            }

            var contents = template.Replace("%ITEMS", itemsHtml.ToString());
            return new HtmlBackup(contents);
        }
    }
}