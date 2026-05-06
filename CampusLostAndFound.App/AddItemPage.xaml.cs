using System.Text;
using System.Text.Json;

namespace CampusLostAndFound.App
{
    public partial class AddItemPage : ContentPage
    {
        private string _uploadedFileName = "yok.jpg";
        private string _adminLocation;

        private readonly List<string> _highValueKeywords = new List<string>
        {
            "telefon", "bilgisayar", "laptop", "tablet", "ipad", "macbook", "dizüstü",
            "kulaklık", "airpods", "akıllı saat", "apple watch", "kamera",
            "takı", "yüzük", "kolye", "küpe", "bileklik", "saat",
            "pırlanta", "altın", "gümüş", "elmas",
            "cüzdan", "para", "nakit", "dolar", "euro",
            "kimlik", "pasaport", "ehliyet", "kredi kartı", "kart"
        };

        private readonly List<string> _brandKeywords = new List<string>
        {
            "apple", "iphone", "macbook", "ipad", "airpods", "samsung", "galaxy",
            "xiaomi", "huawei", "oppo", "lenovo", "asus", "hp", "dell", "monster",
            "rolex", "gucci", "prada", "louis vuitton", "dior", "chanel",
            "hermes", "cartier", "balenciaga", "casio", "zara", "nike", "adidas"
        };

        public AddItemPage(string adminLocation)
        {
            InitializeComponent();
            _adminLocation = adminLocation;

            InstitutionPicker.SelectedItem = _adminLocation;
            InstitutionPicker.IsEnabled = false;
        }

        private bool IsHighValueItem()
        {
            string title = TitleEntry.Text?.ToLower() ?? "";
            return _highValueKeywords.Any(keyword => title.Contains(keyword));
        }

        private bool HasBrandInfo()
        {
            string title = TitleEntry.Text?.ToLower() ?? "";
            return _brandKeywords.Any(brand => title.Contains(brand));
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            if (IsHighValueItem() || HasBrandInfo())
            {
                await DisplayAlert("Güvenlik Uyarısı", "Telefon, bilgisayar, takı gibi yüksek değerli eşyaların fotoğrafları güvenlik amacıyla sisteme yüklenemez.", "Anladım");
                return;
            }

            try
            {
                var result = await MediaPicker.Default.PickPhotoAsync();
                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    SelectedImage.Source = ImageSource.FromStream(() => stream);

                    var content = new MultipartFormDataContent();
                    var fileStream = await result.OpenReadAsync();
                    var fileContent = new StreamContent(fileStream);
                    content.Add(fileContent, "file", result.FileName);

                    HttpClient client = new HttpClient();
                    var response = await client.PostAsync("https://localhost:7293/api/Items/upload-image", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(responseString);
                        _uploadedFileName = data!["fileName"];
                        await DisplayAlert("Başarılı", "Fotoğraf yüklendi!", "Tamam");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Fotoğraf seçilemedi: " + ex.Message, "Tamam");
            }
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            if (HasBrandInfo())
            {
                await DisplayAlert("Kritik Hata", "Lütfen ilan başlığına MARKA/MODEL (Apple, iPhone, Rolex vb.) yazmayın! Bu detayları sadece 'Güvenlik (Gizli Özellikler)' kısmına eklemelisiniz.", "Düzelt");
                return;
            }

            if (IsHighValueItem())
            {
                _uploadedFileName = "yok.jpg";
            }

            var newItem = new
            {
                title = TitleEntry.Text,
                description = "",
                category = _adminLocation,
                location = LocationEntry.Text,
                finderName = FinderEntry.Text,
                secretQuestion = "",
                secretAnswer = SecretFeaturesEntry.Text,
                imageUrl = _uploadedFileName,
                isHandedOver = false
            };

            try
            {
                HttpClient client = new HttpClient();
                var json = JsonSerializer.Serialize(newItem);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://localhost:7293/api/Items", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Başarılı", "Eşya sisteme kaydedildi!", "Tamam");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Hata", "Kaydedilirken bir sorun oluştu.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Bağlantı hatası: " + ex.Message, "Tamam");
            }
        }
    }
}