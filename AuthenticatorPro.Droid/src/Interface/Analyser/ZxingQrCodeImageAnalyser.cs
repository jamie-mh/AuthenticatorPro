// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if FDROID

using Android.Graphics;
using AndroidX.Camera.Core;
using System;
using System.Collections.Generic;
using ZXing;
using ZXing.Common;

namespace AuthenticatorPro.Droid.Interface.Analyser
{
    public class ZxingQrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public event EventHandler<string> QrCodeScanned;

        private readonly BarcodeReader<Bitmap> _barcodeReader;

        public ZxingQrCodeImageAnalyser()
        {
            _barcodeReader = new BarcodeReader<Bitmap>(null, null, ls => new HybridBinarizer(ls))
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    TryInverted = true
                }
            };
        }

        public void Analyze(IImageProxy imageProxy)
        {
            if (imageProxy.Image == null)
            {
                return;
            }

            try
            {
                Analyse(imageProxy);
            }
            finally
            {
                imageProxy.Close();
            }
        }

        private void Analyse(IImageProxy imageProxy)
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
        }
    }
}

#endif