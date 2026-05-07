using System.Text.Json;
using System.Linq;

namespace CampusLostAndFound.App
{
    public partial class MainPage : ContentPage
    {
        // Tüm uygulama genelinde giriş yapılıp yapılmadığını tutan statik (kalıcı) değişkenler
        public static bool GlobalIsAdmin = false;
        public static string GlobalAdminLocation = string.Empty;

        private List<Item> _allItems = new List<Item>();
        private int _itemToHandoverId;

        public MainPage()
        {
            InitializeComponent();
            FilterPicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Eğer LoginPage üzerinden giriş yapıldıysa, arayüzü görevliye göre evrimleştir:
            if (GlobalIsAdmin)
            {
                CorporateLoginButton.IsVisible = false; // Giriş butonunu sakla
                FilterPicker.IsVisible = false; // Filtreyi sakla
                AdminAddButton.IsVisible = true; // Eşya ekle butonunu göster
                PageTitleLabel.Text = $"{GlobalAdminLocation} Paneli"; // Başlığı kuruma özel yap
            }

            // Verileri her açılışta güncel yetkiyle çek
            await LoadItemsFromApi();
        }

        private async void OnCorporateLoginClicked(object sender, EventArgs e)
        {
            // Gizli giriş sayfasına yönlendir
            await Navigation.PushAsync(new LoginPage());
        }

        private async Task LoadItemsFromApi()
        {
            try
            {
                HttpClient client = new HttpClient();
                string response = await client.GetStringAsync("https://localhost:7293/api/Items");
                var items = JsonSerializer.Deserialize<List<Item>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                foreach (var item in items)
                {
                    item.IsAdminMode = GlobalIsAdmin;
                }

                _allItems = items;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Hata", "Veriler çekilemedi: " + ex.Message, "Tamam");
            }
        }

        private async void OnAddButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddItemPage(GlobalAdminLocation));
        }

        private void ApplyFilters()
        {
            var keyword = ItemSearchBar.Text?.ToLower() ?? string.Empty;
            var filteredItems = _allItems.AsEnumerable();

            if (GlobalIsAdmin)
            {
                filteredItems = filteredItems.Where(i => i.category == GlobalAdminLocation);
            }
            else
            {
                var selectedLocation = FilterPicker.SelectedItem?.ToString();
                if (selectedLocation != null && selectedLocation != "Tüm Lokasyonlar")
                {
                    filteredItems = filteredItems.Where(i => i.category == selectedLocation);
                }
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filteredItems = filteredItems.Where(i =>
                    (i.title != null && i.title.ToLower().Contains(keyword)) ||
                    (i.location != null && i.location.ToLower().Contains(keyword)));
            }

            ItemsCollectionView.ItemsSource = filteredItems.ToList();
        }

        private void OnFilterChanged(object sender, EventArgs e) => ApplyFilters();
        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (!GlobalIsAdmin) return;
            var tappedItem = e.Parameter as Item;
            if (tappedItem != null)
            {
                string features = string.IsNullOrWhiteSpace(tappedItem.secretAnswer) ? "Gizli özellik girilmemiş." : tappedItem.secretAnswer;
                await DisplayAlert($"Detaylar: {tappedItem.title}", $"Kurum: {tappedItem.category}\nBulan: {tappedItem.finderName}\nBulunan Yer: {tappedItem.location}\n\nGİZLİ ÖZELLİKLER:\n{features}", "Kapat");
            }
        }

        private void OnHandOverButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            _itemToHandoverId = (int)button.CommandParameter;
            ConsentCheckBox.IsChecked = false;
            HandoverOverlay.IsVisible = true;
        }

        private async void OnConfirmHandoverAction(object sender, EventArgs e)
        {
            if (!ConsentCheckBox.IsChecked)
            {
                await DisplayAlert("Uyarı", "Onay vermelisiniz.", "Tamam");
                return;
            }
            try
            {
                HttpClient client = new HttpClient();
                var response = await client.DeleteAsync($"https://localhost:7293/api/Items/{_itemToHandoverId}");
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Başarılı", "Eşya teslim edildi.", "Tamam");
                    HandoverOverlay.IsVisible = false;
                    await LoadItemsFromApi();
                }
            }
            catch (Exception ex) { await DisplayAlert("Hata", ex.Message, "Tamam"); }
        }

        private void OnCancelHandoverAction(object sender, EventArgs e) => HandoverOverlay.IsVisible = false;
    }

    public class Item
    {
        public int id { get; set; }
        public string title { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string finderName { get; set; } = string.Empty;
        public string imageUrl { get; set; } = "yok.jpg";
        public string FullImageUrl => $"https://localhost:7293/uploads/{imageUrl}";
        public string secretAnswer { get; set; } = string.Empty;
        public bool IsAdminMode { get; set; }
    }
}