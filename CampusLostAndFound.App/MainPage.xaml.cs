using System.Text.Json;
using System.Linq;
using System;
using Microsoft.Maui.Graphics;

namespace CampusLostAndFound.App
{
    public partial class MainPage : ContentPage
    {
        public static bool GlobalIsAdmin = false;
        public static string GlobalAdminLocation = string.Empty;

        private List<Item> _allItems = new List<Item>();
        private int _itemToHandoverId;

        public MainPage()
        {
            InitializeComponent();
            FilterPicker.SelectedIndex = 0;
        }

        public MainPage(string adminLocation)
        {
            InitializeComponent();
            FilterPicker.SelectedIndex = 0;

            GlobalIsAdmin = true;
            GlobalAdminLocation = adminLocation;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (GlobalIsAdmin)
            {
                CorporateLoginButton.IsVisible = false;
                FilterPicker.IsVisible = false;
                AdminAddButton.IsVisible = true;
                PageTitleLabel.Text = $"{GlobalAdminLocation} Paneli";
            }

            await LoadItemsFromApi();
            UpdateContactInfo();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadItemsFromApi();
            ItemsRefreshView.IsRefreshing = false;
        }

        // DEĞİŞEN KISIM BURASI: Görevli panelinde bilgi kutusu gizlendi
        private void UpdateContactInfo()
        {
            // Eğer görevli girişi yapıldıysa bilgi kutusunu gizle ve metottan çık
            if (GlobalIsAdmin)
            {
                ContactInfoContainer.IsVisible = false;
                return;
            }

            // Burası sadece öğrenciler (normal kullanıcılar) için çalışacak
            string selectedLocation = FilterPicker.SelectedItem?.ToString();

            if (selectedLocation == "Maltepe Üniversitesi")
            {
                ContactInfoLabel.Text = "Kayıp Eşya Ofisi: 0216 626 10 50\nDahili: 2222 (Güvenlik Merkezi)";
                ContactInfoContainer.IsVisible = true;
            }
            else if (selectedLocation == "City's AVM")
            {
                ContactInfoLabel.Text = "Danışma: 0212 373 33 33\nKonum: Zemin Kat No: 12";
                ContactInfoContainer.IsVisible = true;
            }
            else if (selectedLocation == "Marmaray")
            {
                ContactInfoLabel.Text = "Çözüm Merkezi: 444 82 33\nE-posta: cozum@tcddtasimacilik.gov.tr";
                ContactInfoContainer.IsVisible = true;
            }
            else
            {
                ContactInfoContainer.IsVisible = false;
            }
        }

        private async void OnCorporateLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }

        private async Task LoadItemsFromApi()
        {
            try
            {
                HttpClient client = new HttpClient();
                string response = await client.GetStringAsync("http://localhost:5280/api/Items");
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

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
            UpdateContactInfo();
        }

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
                var response = await client.DeleteAsync($"http://localhost:5280/api/Items/{_itemToHandoverId}");
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

        public DateTime createdDate { get; set; }

        public string FullImageUrl => $"http://localhost:5280/api/Items/proxy/{imageUrl}";

        public bool IsOverOneYear => createdDate != default && (DateTime.Now - createdDate).TotalDays > 365;
        public string WarningText => "⚠️ 1 yıldan uzun süredir kayıp (Açık Artırma/Arşiv statüsünde)";

        public Color StatusColor
        {
            get
            {
                if (createdDate == default) return Colors.Transparent;

                var totalDays = (DateTime.Now - createdDate).TotalDays;
                if (totalDays > 365) return Colors.DarkRed;
                if (totalDays > 180) return Colors.Red;
                if (totalDays > 90) return Colors.DarkOrange;
                return Colors.Transparent;
            }
        }

        public string secretAnswer { get; set; } = string.Empty;
        public bool IsAdminMode { get; set; }
    }
}