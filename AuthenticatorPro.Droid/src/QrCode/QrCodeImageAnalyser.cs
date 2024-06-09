// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Util;
using AndroidX.Camera.Core;
using AuthenticatorPro.ZXing;
using Serilog;
using ImageFormat = AuthenticatorPro.ZXing.ImageFormat;
using Log = Serilog.Log;

namespace AuthenticatorPro.Droid.QrCode
{
    public class QrCodeImageAnalyser : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public event EventHandler<string> QrCodeScanned;
        public Size DefaultTargetResolution => new(640, 480);
        
        private readonly ILogger _log = Log.ForContext<QrCodeImageAnalyser>();

        private readonly QrCodeReader _qrCodeReader = new(new ReaderOptions
        {
            TryRotate = true,
            TryHarder = true,
            Binarizer = Binarizer.LocalAverage
        });
        
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
            using var plane = imageProxy.Image.GetPlanes()[0];

            var bytes = new byte[plane.Buffer.Capacity()];
            plane.Buffer.Get(bytes);
            
            using var imageView = new ImageView(bytes, imageProxy.Width, imageProxy.Height, ImageFormat.Lum, plane.RowStride, plane.PixelStride);
            string result;
            
            try
            {
                result = _qrCodeReader.Read(imageView);
            }
            catch (QrCodeException e)
            {
                _log.Warning(e, "Error scanning QR code: {Type}", e.Type);
                return;
            }

            if (result != null)
            {
                QrCodeScanned?.Invoke(this, result);
            }
        }
    }
}
