using System.Text;
using System.Text.Json;

namespace CampusLostAndFound.App
{
    public partial class AddItemPage : ContentPage
    {
        private string _uploadedFileName = "yok.jpg";
        private string _adminLocation;

        // SADECE FOTOĞRAF YÜKLEMEYİ ENGELLEYENLER (Başlığa yazmak serbest)
        private readonly List<string> _noPhotoKeywords = new List<string>
        {
            "telefon", "bilgisayar", "laptop", "tablet", "ipad", "macbook", "dizüstü",
            "airpods", "akıllı saat", "apple watch", "kamera",
            "pırlanta", "altın", "gümüş", "elmas"
        };

        // BAŞLIĞA YAZILMASI KESİNLİKLE YASAK OLANLAR (Sadece çok değerli materyaller)
        private readonly List<string> _forbiddenTitleKeywords = new List<string>
        {
            "pırlanta", "altın", "gümüş", "elmas"
        };

        // BAŞLIKTA YASAK OLAN MARKALAR
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

        private bool IsPhotoRestricted()
        {
            string title = TitleEntry.Text?.ToLower() ?? "";
            string secretFeatures = SecretFeaturesEntry.Text?.ToLower() ?? "";

            return _noPhotoKeywords.Any(keyword => title.Contains(keyword) || secretFeatures.Contains(keyword));
        }

        private bool HasBrandInfo()
        {
            string title = TitleEntry.Text?.ToLower() ?? "";
            string secretFeatures = SecretFeaturesEntry.Text?.ToLower() ?? "";

            return _brandKeywords.Any(brand => title.Contains(brand) || secretFeatures.Contains(brand));
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            if (IsPhotoRestricted() || HasBrandInfo())
            {
                await DisplayAlert("Güvenlik Uyarısı", "Telefon, bilgisayar gibi cihazların veya değerli materyallerin fotoğrafları güvenlik amacıyla sisteme yüklenemez.", "Anladım");
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

            // 1. Marka Kontrolü (Başlıkta yasak)
            if (_brandKeywords.Any(brand => titleText.Contains(brand)))
            {
                await DisplayAlert("Kritik Hata", "Lütfen ilan başlığına MARKA/MODEL yazmayın! Bu detayları sadece 'Güvenlik (Gizli Özellikler)' kısmına eklemelisiniz.", "Düzelt");
                return;
            }

            // 2. Çok Değerli Materyal Kontrolü (Altın, pırlanta vb. başlıkta yasak)
            if (_forbiddenTitleKeywords.Any(keyword => titleText.Contains(keyword)))
            {
                await DisplayAlert("Güvenlik Uyarısı", "İlan başlığına değerli materyal ismi (altın, pırlanta vb.) yazamazsınız! Lütfen başlığa 'Takı/Eşya' gibi genel bir isim yazıp, asıl detayları 'Güvenlik' kısmına ekleyin.", "Düzelt");
                return;
            }

            // 3. Fotoğraf Yükleme İzni Kontrolü
            if (IsPhotoRestricted())
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