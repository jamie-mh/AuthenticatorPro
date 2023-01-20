// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Gms.Extensions;
using Android.Runtime;
using AndroidX.Camera.Core;
using System;
using System.Linq;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;

namespace AuthenticatorPro.Droid
{
    public class QrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const int ScanInterval = 500;

        public event EventHandler<Barcode> QrCodeScanned;

        private readonly IBarcodeScanner _barcodeScanner;

        private long _lastScanMillis;

        public QrCodeImageAnalyser()
        {
            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();

            _barcodeScanner = BarcodeScanning.GetClient(options);
        }

        public async void Analyze(IImageProxy imageProxy)
        {
            var image = imageProxy.Image;

            if (image == null)
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
                var inputImage = InputImage.FromMediaImage(image, imageProxy.ImageInfo.RotationDegrees);
                var barcodes = await _barcodeScanner.Process(inputImage).AsAsync<JavaList<Barcode>>();

                if (barcodes.Any())
                {
                    QrCodeScanned?.Invoke(this, barcodes[0]);
                }
            }
            finally
            {
                imageProxy.Close();
            }
        }
    }
}