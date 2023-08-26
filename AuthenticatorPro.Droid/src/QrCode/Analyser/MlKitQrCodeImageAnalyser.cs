// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if !FDROID

using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using Android.Util;
using AndroidX.Camera.Core;
using Java.Lang;
using Xamarin.Google.MLKit.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;
using Object = Java.Lang.Object;

namespace AuthenticatorPro.Droid.QrCode.Analyser
{
    public class MlKitQrCodeImageAnalyser : Object, ImageAnalysis.IAnalyzer
    {
        private const int ScanInterval = 500;
        private readonly IBarcodeScanner _barcodeScanner;

        private long _lastScanMillis;

        public MlKitQrCodeImageAnalyser(Context context)
        {
            try
            {
                MlKit.Initialize(context);
            }
            catch (IllegalStateException e)
            {
                Logger.Warn("MlKit already initialised", e);
            }

            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();

            _barcodeScanner = BarcodeScanning.GetClient(options);
        }

        public Size DefaultTargetResolution => new(1920, 1080);

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

        public event EventHandler<string> QrCodeScanned;

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