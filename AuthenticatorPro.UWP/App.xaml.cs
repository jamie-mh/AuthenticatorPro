using System;
using AuthenticatorPro.UWP.Data;
using SQLite;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace AuthenticatorPro.UWP
{
    sealed partial class App : Application
    {
        public static SQLiteAsyncConnection Connection { get; private set; }


        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Connection = await Database.GetConnection(null);

            if(Window.Current.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if(e.PrelaunchActivated == false)
            {
                if(rootFrame.Content == null)
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);

                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            await Connection.CloseAsync();
            e.SuspendingOperation.GetDeferral().Complete();
        }
    }
}
