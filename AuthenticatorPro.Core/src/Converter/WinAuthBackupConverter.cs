// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Backup;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Converter
{
    public class WinAuthBackupConverter : UriListBackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Maybe;

        public WinAuthBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override async Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            if (password == null)
            {
                return await base.ConvertAsync(data, null);
            }

            using var inputMemory = new MemoryStream(data);
            using var zip = new ZipFile(inputMemory);
            zip.Password = password;

            var fileEntry = zip.Cast<ZipEntry>().FirstOrDefault(entry => entry.IsFile);

            if (fileEntry == null)
            {
                throw new ArgumentException("No file found in zip");
            }

            using var outputMemory = new MemoryStream();

            try
            {
                await using var zipStream = zip.GetInputStream(fileEntry);
                var buffer = new byte[4096];
                StreamUtils.Copy(zipStream, outputMemory, buffer);
            }
            catch (ZipException e)
            {
                throw new ArgumentException("Invalid password", e);
            }

            return await base.ConvertAsync(outputMemory.ToArray(), null);
        }
    }
}