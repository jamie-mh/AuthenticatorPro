// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QRCoder;
using Stratum.Core.Entity;

namespace Stratum.Core.Backup
{
    public class HtmlBackup
    {
        public const string FileExtension = "html";
        public const string MimeType = "text/html";

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

        private static Task<string> GetTemplate(IAssetProvider assetProvider)
        {
            return assetProvider.ReadStringAsync(BackupTemplateFileName);
        }

        public static async Task<HtmlBackup> FromAuthenticators(IAssetProvider assetProvider,
            IEnumerable<Authenticator> authenticators)
        {
            var template = await GetTemplate(assetProvider);

            var itemsHtml = new StringBuilder();
            var generator = new QRCodeGenerator();

            Task<string> GetQrCodeDataAsync(string uri)
            {
                return Task.Run(() =>
                {
                    var qrCodeData = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new PngByteQRCode(qrCodeData);

                    var bytes = qrCode.GetGraphic(PixelsPerModule);
                    return Convert.ToBase64String(bytes);
                });
            }

            foreach (var auth in authenticators)
            {
                string uri;

                try
                {
                    uri = auth.GetUri();
                }
                catch (NotSupportedException)
                {
                    continue;
                }

                var qrCode = await GetQrCodeDataAsync(uri);

                itemsHtml.Append($"""
                                  <tr>
                                      <td>{auth.Issuer}</td>
                                      <td>{auth.Username}</td>
                                      <td><code>{uri}</code></td>
                                      <td><img src="data:image/png;base64,{qrCode}"></td>
                                  </tr>
                                  """);
            }

            var contents = template.Replace("%ITEMS", itemsHtml.ToString());
            return new HtmlBackup(contents);
        }
    }
}