// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Droid.Shared.Util;
using Google.Android.Material.Button;
using QRCoder;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class QrCodeBottomSheet : BottomSheet
    {
        private const int PixelsPerModule = 4;

        private ImageView _image;
        private ProgressBar _progressBar;

        private string _uri;
        
        public QrCodeBottomSheet() : base(Resource.Layout.sheetQrCode) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _uri = Arguments.GetString("uri");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.qrCode, true);

            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);
            _image = view.FindViewById<ImageView>(Resource.Id.imageQrCode);

            var okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            okButton.Click += delegate { Dismiss(); };

            var copyButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCopyUri);
            copyButton.Click += delegate
            {
                var clipboard = (ClipboardManager) Context.GetSystemService(Context.ClipboardService);
                var clip = ClipData.NewPlainText("uri", _uri);
                clipboard.PrimaryClip = clip;
                Toast.MakeText(Context, Resource.String.uriCopiedToClipboard, ToastLength.Short).Show();
            };
            
            return view;
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            var ppm = (int) Math.Floor(PixelsPerModule * Resources.DisplayMetrics.Density);
            
            var bytes = await Task.Run(delegate
            {
                var generator = new QRCodeGenerator();
                var qrCodeData = generator.CreateQrCode(_uri, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new BitmapByteQRCode(qrCodeData);
                return qrCode.GetGraphic(ppm);
            });

            var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
           
            AnimUtil.FadeOutView(_progressBar, AnimUtil.LengthShort);
            AnimUtil.FadeInView(_image, AnimUtil.LengthLong);
            _image.SetImageBitmap(bitmap);
        }
    }
}