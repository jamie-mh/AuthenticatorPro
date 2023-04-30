// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AuthenticatorPro.Droid.Interface;
using AuthenticatorPro.Droid.Interface.Analyser;
using Google.Android.Material.Button;
using Java.Util.Concurrent;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class ScanActivity : BaseActivity
    {
        public ScanActivity() : base(Resource.Layout.activityScan)
        {
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var previewView = FindViewById<PreviewView>(Resource.Id.previewView);
            var flashButton = FindViewById<MaterialButton>(Resource.Id.buttonFlash);

            var provider = (ProcessCameraProvider) await ProcessCameraProvider.GetInstance(this).GetAsync();

            var preview = new Preview.Builder().Build();
            var selector = new CameraSelector.Builder()
                .RequireLensFacing(CameraSelector.LensFacingBack)
                .Build();

            preview.SetSurfaceProvider(previewView.SurfaceProvider);

#if FDROID
            var analysis = new ImageAnalysis.Builder()
                .SetTargetResolution(new Size(1280, 720))
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888)
                .Build();
            
            var analyser = new ZxingQrCodeImageAnalyser();
            analyser.QrCodeScanned += OnQrCodeScanned;
            analysis.SetAnalyzer(Executors.NewSingleThreadExecutor(), analyser);
#else
            var analysis = new ImageAnalysis.Builder()
                .SetTargetResolution(new Size(1920, 1080))
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .Build();
            
            var analyser = new MlKitQrCodeImageAnalyser();
            analyser.QrCodeScanned += OnQrCodeScanned;
            analysis.SetAnalyzer(ContextCompat.GetMainExecutor(this), analyser);
#endif
            
            var camera = provider.BindToLifecycle(this, selector, analysis, preview);
            var isFlashOn = false;

            flashButton.Click += (_, _) =>
            {
                isFlashOn = !isFlashOn;
                camera.CameraControl.EnableTorch(isFlashOn);
            };
        }

        private void OnQrCodeScanned(object sender, string qrCode)
        {
            var intent = new Intent();
            intent.PutExtra("text", qrCode);
            SetResult(Result.Ok, intent);
            Finish();
        }
    }
}