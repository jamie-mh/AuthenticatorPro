// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AndroidX.Camera.Core;
using System;
using System.Threading.Tasks;

#if FDROID
using Android.Graphics;
using ZXing;
using ZXing.Common;
using System.Collections.Generic;
#else
using System.Linq;
using Android.Runtime;
using Android.Gms.Extensions;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;
#endif

namespace AuthenticatorPro.Droid.Interface
{
    public class QrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const int ScanInterval = 500;

        public event EventHandler<string> QrCodeScanned;

        private long _lastScanMillis;

#if FDROID
        private readonly BarcodeReader<Bitmap> _barcodeReader;
#else
        private readonly IBarcodeScanner _barcodeScanner;
#endif

        public QrCodeImageAnalyser()
        {
#if FDROID
            _barcodeReader = new BarcodeReader<Bitmap>(null, null, ls => new GlobalHistogramBinarizer(ls))
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    TryInverted = true
                }
            };
#else
            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();

            _barcodeScanner = BarcodeScanning.GetClient(options);
#endif
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

#if FDROID
        private async Task AnalyseAsync(IImageProxy imageProxy)
        {
            await Task.Run(delegate
            {
                var rgbaPlane = imageProxy.Image.GetPlanes()[0];

                var bytes = new byte[rgbaPlane.Buffer.Remaining()];
                rgbaPlane.Buffer.Get(bytes);

                var source = new RGBLuminanceSource(bytes, imageProxy.Width, imageProxy.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
                var result = _barcodeReader.Decode(source);

                if (result != null)
                {
                    QrCodeScanned?.Invoke(this, result.Text);
                }
            });
        }
#else
        private async Task AnalyseAsync(IImageProxy imageProxy)
        {
            var inputImage = InputImage.FromMediaImage(imageProxy.Image, imageProxy.ImageInfo.RotationDegrees);
            var barcodes = await _barcodeScanner.Process(inputImage).AsAsync<JavaList<Barcode>>();

            if (barcodes.Any())
            {
                QrCodeScanned?.Invoke(this, barcodes[0].RawValue);
            }
        }
#endif
    }
}