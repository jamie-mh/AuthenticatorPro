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
using Java.Util.Concurrent;
using Xamarin.Google.MLKit.Vision.Barcode.Common;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class ScanActivity : BaseActivity
    {
        private PreviewView _previewView;

        public ScanActivity() : base(Resource.Layout.activityScan)
        {
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _previewView = FindViewById<PreviewView>(Resource.Id.previewView);

            var provider = (ProcessCameraProvider) await ProcessCameraProvider.GetInstance(this).GetAsync();

            var preview = new Preview.Builder().Build();
            var selector = new CameraSelector.Builder()
                .RequireLensFacing(CameraSelector.LensFacingBack)
                .Build();

            preview.SetSurfaceProvider(_previewView.SurfaceProvider);

            var analysis = new ImageAnalysis.Builder()
                .SetTargetResolution(new Size(1920, 1080))
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .Build();

            var analyser = new QrCodeImageAnalyser();
            analyser.QrCodeScanned += OnQrCodeScanned;

            analysis.SetAnalyzer(ContextCompat.GetMainExecutor(this), analyser);
            var camera = provider.BindToLifecycle(this, selector, analysis, preview);
        }

        private void OnQrCodeScanned(object sender, Barcode qrCode)
        {
            var intent = new Intent();
            intent.PutExtra("text", qrCode.RawValue);
            SetResult(Result.Ok, intent);
            Finish();
        }
    }
}