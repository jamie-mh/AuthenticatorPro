// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if !FDROID

using Android.Gms.Extensions;
using Android.Runtime;
using Android.Util;
using AndroidX.Camera.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;

namespace AuthenticatorPro.Droid.QrCode.Analyser
{
    public class MlKitQrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public event EventHandler<string> QrCodeScanned;
        public Size DefaultTargetResolution => new(1920, 1080);
        
        private const int ScanInterval = 500;

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