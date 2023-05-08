// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if !FDROID

using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using AuthenticatorPro.Droid.QrCode.Reader;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.QrCode.Reader
{
    public class MlKitQrCodeReader : IQrCodeReader
    {
        public async Task<string> ScanImageFromFileAsync(Context context, Uri uri)
        {
            InputImage image;

            try
            {
                image = await Task.Run(() => InputImage.FromFilePath(context, uri));
            }
            catch (Exception e)
            {
                throw new IOException("Failed to read file", e);
            }

            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();

            var scanner = BarcodeScanning.GetClient(options);
            var barcodes = await scanner.Process(image).AsAsync<JavaList<Barcode>>();

            return barcodes
                .Select(b => b.RawValue)
                .FirstOrDefault();
        }
    }
}

#endif