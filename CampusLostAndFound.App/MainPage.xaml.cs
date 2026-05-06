using System.Text.Json;
using System.Linq;

namespace CampusLostAndFound.App
{
    public partial class MainPage : ContentPage
    {
        private List<Item> _allItems = new List<Item>();
        private bool _isUserLoggedIn = false;
        private bool _isAdmin = false;
        private string _adminLocation = string.Empty;
        private int _itemToHandoverId;

        public MainPage()
        {
            InitializeComponent();
            FilterPicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadItemsFromApi();

            if (_isUserLoggedIn) return;

            bool isAdminChoice = await DisplayAlert("Hoş Geldiniz", "Sisteme nasıl giriş yapmak istiyorsunuz?", "Görevli", "Kullanıcı");

            if (isAdminChoice)
            {
                LoginOverlay.IsVisible = true;
            }
            else
            {
                _isUserLoggedIn = true;
            }
        }

        private async void OnLoginConfirmClicked(object sender, EventArgs e)
        {
            if (PasswordEntry.Text == "maltepe") _adminLocation = "Maltepe Üniversitesi";
            else if (PasswordEntry.Text == "citys") _adminLocation = "City's AVM";
            else if (PasswordEntry.Text == "marmaray") _adminLocation = "Marmaray";
            else _adminLocation = string.Empty;

            if (!string.IsNullOrEmpty(_adminLocation))
            {
                LoginOverlay.IsVisible = false;
                AdminAddButton.IsVisible = true;

                FilterPicker.IsVisible = false;
                PageTitleLabel.Text = $"{_adminLocation} Paneli";

                _isAdmin = true;
                _isUserLoggedIn = true;

                // ÇÖZÜM BURADA: Sadece filtreyi uygulamak yerine verileri görevli yetkisiyle baştan yüklüyoruz.
                // Böylece "Sahibine Teslim Et" butonları anında görünür hale geliyor.
                await LoadItemsFromApi();

                PasswordEntry.Text = string.Empty;
            }
            else
            {
                await DisplayAlert("Hata", "Hatalı şifre girdiniz!", "Tamam");
                PasswordEntry.Text = string.Empty;
            }
        }

        private void OnLoginCancelClicked(object sender, EventArgs e)
        {
            LoginOverlay.IsVisible = false;
            PasswordEntry.Text = string.Empty;
            _isUserLoggedIn = true;
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
                    item.IsAdminMode = _isAdmin;
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
            await Navigation.PushAsync(new AddItemPage(_adminLocation));
        }

        private void ApplyFilters()
        {
            var keyword = ItemSearchBar.Text?.ToLower() ?? string.Empty;
            var filteredItems = _allItems.AsEnumerable();

            if (_isAdmin)
            {
                filteredItems = filteredItems.Where(i => i.category == _adminLocation);
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
            if (!_isAdmin) return;
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