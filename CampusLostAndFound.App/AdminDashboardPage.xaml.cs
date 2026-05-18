using System.Text.Json;

namespace CampusLostAndFound.App
{
    public partial class AdminDashboardPage : ContentPage
    {
        private string _adminLocation;

        public AdminDashboardPage(string adminLocation)
        {
            InitializeComponent();
            _adminLocation = adminLocation;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadStatistics();
        }

        private async Task LoadStatistics()
        {
            try
            {
                HttpClient client = new HttpClient();
                // EMÜLATÖR İÇİN 10.0.2.2 OLARAK GÜNCELLENDİ
                var response = await client.GetAsync($"http://10.0.2.2:5280/api/Items/statistics/{_adminLocation}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var stats = JsonSerializer.Deserialize<StatisticsResponse>(json, options);

                    if (stats != null)
                    {
                        ActiveItemsLabel.Text = stats.TotalActiveItems.ToString();
                        DeliveredItemsLabel.Text = stats.TotalDeliveredItems.ToString();
                        ArchivedItemsLabel.Text = stats.TotalArchivedItems.ToString();

                        LoadingLabel.IsVisible = false;
                        LocationsStackLayout.Children.Clear();

                        if (stats.FrequentLocations != null && stats.FrequentLocations.Count > 0)
                        {
                            foreach (var loc in stats.FrequentLocations)
                            {
                                var locLabel = new Label
                                {
                                    Text = $"📍 {loc.Location} - {loc.Count} eşya",
                                    TextColor = Colors.White,
                                    FontSize = 16
                                };
                                LocationsStackLayout.Children.Add(locLabel);
                            }
                        }
                        else
                        {
                            LocationsStackLayout.Children.Add(new Label { Text = "Henüz yeterli veri yok.", TextColor = Colors.Gray });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "İstatistikler yüklenemedi: " + ex.Message, "Tamam");
            }
        }

        private async void OnGoToListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage(_adminLocation));
        }
    }

    public class StatisticsResponse
    {
        public int TotalActiveItems { get; set; }
        public int TotalDeliveredItems { get; set; }
        public int TotalArchivedItems { get; set; }
        public List<LocationStat> FrequentLocations { get; set; }
    }

    public class LocationStat
    {
        public string Location { get; set; }
        public int Count { get; set; }
    }
}