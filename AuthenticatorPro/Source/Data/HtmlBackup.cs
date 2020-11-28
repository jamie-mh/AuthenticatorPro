using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using OtpNet;
using QRCoder;

namespace AuthenticatorPro.Data
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

        private static string GetOtpAuthUrl(Authenticator auth)
        {
            var type = auth.Type switch
            {
                AuthenticatorType.Hotp => "hotp",
                AuthenticatorType.Totp => "totp",
                _ => throw new ArgumentException()
            };

            var algorithm = auth.Algorithm switch
            {
                OtpHashMode.Sha1 => "SHA1",
                OtpHashMode.Sha256 => "SHA256",
                OtpHashMode.Sha512 => "SHA512",
                _ => throw new ArgumentException()
            };
            
            var issuerUsername = auth.Username == "" ? auth.Issuer : $"{auth.Issuer}:{auth.Username}";
            
            var url =
                $"otpauth://{type}/{Uri.EscapeDataString(issuerUsername)}?secret={auth.Secret}&issuer={Uri.EscapeDataString(auth.Issuer)}&algorithm={algorithm}&digits={auth.Digits}";

            if(auth.Type == AuthenticatorType.Totp)
                url += $"&period={auth.Period}";
            else if(auth.Type == AuthenticatorType.Hotp)
                url += $"&counter={auth.Counter}";

            return url;
        }

        public static async Task<HtmlBackup> FromAuthenticatorList(Context context, List<Authenticator> authenticators)
        {
            var template = await GetTemplate(context);
            
            var itemsHtml = new StringBuilder();
            var generator = new QRCodeGenerator();
            
            async Task<string> GetQrCodeDataAsync(string url)
            {
                return await Task.Run(() =>
                {
                    var qrCodeData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new PngByteQRCode(qrCodeData);

                    var bytes = qrCode.GetGraphic(PixelsPerModule);
                    return Convert.ToBase64String(bytes);
                });
            }

            foreach(var auth in authenticators)
            {
                var url = GetOtpAuthUrl(auth);
                var qrCode = await GetQrCodeDataAsync(url);

                itemsHtml.Append($@"
                    <tr>
                        <td>{auth.Issuer}</td>
                        <td>{auth.Username}</td>
                        <td><code>{url}</code></td>
                        <td><img src=""data:image/png;base64,{qrCode}""></td>
                    </tr>
                ");
            }

            var contents = template.Replace("%ITEMS", itemsHtml.ToString());
            return new HtmlBackup(contents);
        }
    }
}