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
            string secretFeatures = SecretFeaturesEntry.Text?.ToLower() ?? ""; // Güvenlik alanı da eklendi

            return _highValueKeywords.Any(keyword => title.Contains(keyword) || secretFeatures.Contains(keyword));
        }

        private bool HasBrandInfo()
        {
            string title = TitleEntry.Text?.ToLower() ?? "";
            string secretFeatures = SecretFeaturesEntry.Text?.ToLower() ?? ""; // Güvenlik alanı da eklendi

            return _brandKeywords.Any(brand => title.Contains(brand) || secretFeatures.Contains(brand));
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            if (IsHighValueItem() || HasBrandInfo())
            {
                await DisplayAlert("Güvenlik Uyarısı", "Telefon, bilgisayar, takı gibi yüksek değerli eşyaların fotoğrafları güvenlik amacıyla sisteme yüklenemez.", "Anladım");
                return;
            }

            string action = await DisplayActionSheet("Fotoğraf Ekle", "Vazgeç", null, "Kamerayı Aç", "Galeriden Seç");
            FileResult result = null;

            try
            {
                if (action == "Kamerayı Aç")
                {
                    result = await MediaPicker.Default.CapturePhotoAsync();
                }
                else if (action == "Galeriden Seç")
                {
                    result = await MediaPicker.Default.PickPhotoAsync();
                }
                else
                {
                    return;
                }

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    SelectedImage.Source = ImageSource.FromStream(() => stream);

                    var content = new MultipartFormDataContent();
                    var fileStream = await result.OpenReadAsync();
                    var fileContent = new StreamContent(fileStream);
                    content.Add(fileContent, "file", result.FileName);

                    HttpClient client = new HttpClient();
                    var response = await client.PostAsync("http://localhost:5280/api/Items/upload-image", content);

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
                await DisplayAlert("Hata", "Fotoğraf işlemi başarısız: " + ex.Message, "Tamam");
            }
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            string titleText = TitleEntry.Text?.ToLower() ?? "";

            // 1. Başlıkta Marka Kontrolü
            if (_brandKeywords.Any(brand => titleText.Contains(brand)))
            {
                await DisplayAlert("Kritik Hata", "Lütfen ilan başlığına MARKA/MODEL yazmayın! Bu detayları sadece 'Güvenlik (Gizli Özellikler)' kısmına eklemelisiniz.", "Düzelt");
                return;
            }

            // 2. Başlıkta Lüks/Değerli Eşya Kontrolü (YENİ EKLENEN KISIM)
            if (_highValueKeywords.Any(keyword => titleText.Contains(keyword)))
            {
                await DisplayAlert("Güvenlik Uyarısı", "İlan başlığına lüks veya değerli eşya ismi (altın, pırlanta, telefon, cüzdan vb.) yazamazsınız! Lütfen başlığa 'Değerli Eşya' gibi genel bir isim yazıp, asıl detayları 'Güvenlik' kısmına ekleyin.", "Düzelt");
                return;
            }

            // 3. Fotoğraf Yükleme İzni Kontrolü (Güvenlik kısmına yazılanlar için)
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

                var response = await client.PostAsync("http://localhost:5280/api/Items", content);

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