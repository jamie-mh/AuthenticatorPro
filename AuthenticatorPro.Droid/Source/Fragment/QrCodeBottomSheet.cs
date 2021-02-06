using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AuthenticatorPro.Droid.Shared.Util;
using Google.Android.Material.Button;
using QRCoder;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class QrCodeBottomSheet : BottomSheet
    {
        private const int PixelsPerModule = 12;

        private ImageView _image;
        private ProgressBar _progressBar;

        private readonly Context _context;
        private readonly string _uri;

        public QrCodeBottomSheet(Context context, string uri)
        {
            RetainInstance = true;
            _context = context;
            _uri = uri;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetQrCode, null);
            SetupToolbar(view, Resource.String.qrCode, true);

            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);
            _image = view.FindViewById<ImageView>(Resource.Id.imageQrCode);

            var okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            okButton.Click += delegate { Dismiss(); };

            var copyButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCopyUri);
            copyButton.Click += delegate
            {
                var clipboard = (ClipboardManager) _context.GetSystemService(Context.ClipboardService);
                var clip = ClipData.NewPlainText("uri", _uri);
                clipboard.PrimaryClip = clip;
                Toast.MakeText(_context, Resource.String.uriCopiedToClipboard, ToastLength.Short).Show();
            };
            
            return view;
        }

        public override async void Show(FragmentManager manager, string tag)
        {
            base.Show(manager, tag);
            
            var ppm = (int) Math.Floor(PixelsPerModule * _context.Resources.DisplayMetrics.Density);
            
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