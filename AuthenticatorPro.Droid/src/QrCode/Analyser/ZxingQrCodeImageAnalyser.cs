// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

#if FDROID

using Android.Graphics;
using Android.Util;
using AndroidX.Camera.Core;
using System;
using System.Collections.Generic;
using ZXing;
using ZXing.Common;

namespace AuthenticatorPro.Droid.QrCode.Analyser
{
    public class ZxingQrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public event EventHandler<string> QrCodeScanned;
        public Size DefaultTargetResolution => new(640, 480);

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
                AnalyseInternal(imageProxy);
            }
            finally
            {
                imageProxy.Close();
            }
        }

        private void AnalyseInternal(IImageProxy imageProxy)
        {
            var plane = imageProxy.Image.GetPlanes()[0];

            var bytes = new byte[plane.Buffer.Capacity()];
            plane.Buffer.Get(bytes);

            var source = new PlanarYUVLuminanceSource(
                bytes, imageProxy.Width, imageProxy.Height, 0, 0, imageProxy.Width, imageProxy.Height, false);

            var result = _barcodeReader.Decode(source);

            if (result != null)
            {
                QrCodeScanned?.Invoke(this, result.Text);
            }
        }
    }
}

#endif