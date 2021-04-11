using System.Threading.Tasks;
using AuthenticatorPro.Shared.Source.Data;
using AuthenticatorPro.UWP.Data.Source;
using Windows.UI.Xaml.Controls;
using muxc = Microsoft.UI.Xaml.Controls;

namespace AuthenticatorPro.UWP
{
    public sealed partial class MainPage : Page
    {
        private readonly AuthenticatorSource _source;

        public MainPage()
        {
            _source = new AuthenticatorSource(App.Connection);
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await _source.Update();
        }

        private async void navigationView_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await UpdateCategoriesView();
            navigationView.SelectedItem = navigationView.MenuItems[0];
        }

        private async Task UpdateCategoriesView()
        {
            navigationView.MenuItems.Clear();
            navigationView.MenuItems.Add(new muxc.NavigationViewItem()
            {
                Tag = null,
                Content = "All"
            });

            var categories = await App.Connection.Table<Category>().ToListAsync();
            
            foreach(var category in categories)
            {
                navigationView.MenuItems.Add(new muxc.NavigationViewItem()
                {
                    Tag = category.Id,
                    Content = category.Name
                });
            }
        }

        private void navigationView_SelectionChanged(muxc.NavigationView sender, muxc.NavigationViewSelectionChangedEventArgs args)
        {
            if(args.IsSettingsSelected)
            {
                contentFrame.Visibility = Windows.UI.Xaml.Visibility.Visible;
                itemsRepeater.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                contentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            var categoryId = args.SelectedItemContainer.Tag?.ToString();

            if(_source.CategoryId == categoryId)
                return;

            _source.CategoryId = categoryId;

            contentFrame.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            itemsRepeater.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
    }
}
