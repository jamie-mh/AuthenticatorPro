// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if !FDROID

using AndroidX.Camera.Core;
using System;
using System.Threading.Tasks;
using System.Linq;
using Android.Runtime;
using Android.Gms.Extensions;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;

namespace AuthenticatorPro.Droid.Interface.Analyser
{
    public class MlKitQrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const int ScanInterval = 500;
        public event EventHandler<string> QrCodeScanned;

        private long _lastScanMillis;
        private readonly IBarcodeScanner _barcodeScanner;

        public MlKitQrCodeImageAnalyser()
        {
            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();

            _barcodeScanner = BarcodeScanning.GetClient(options);
        }

        public async void Analyze(IImageProxy imageProxy)
        {
            if (imageProxy.Image == null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (_lastScanMillis > 0 && now - _lastScanMillis <= ScanInterval)
            {
                imageProxy.Close();
                return;
            }

            _lastScanMillis = now;

            try
            {
                await AnalyseAsync(imageProxy);
            }
            finally
            {
                imageProxy.Close();
            }
        }

        private async Task AnalyseAsync(IImageProxy imageProxy)
        {
            var inputImage = InputImage.FromMediaImage(imageProxy.Image, imageProxy.ImageInfo.RotationDegrees);
            var barcodes = await _barcodeScanner.Process(inputImage).AsAsync<JavaList<Barcode>>();

            if (barcodes.Any())
            {
                QrCodeScanned?.Invoke(this, barcodes[0].RawValue);
            }
        }
    }
}

#endif