using System.Text;
using System.Text.Json;

namespace CampusLostAndFound.App
{
    public partial class AddItemPage : ContentPage
    {
        private string _uploadedFileName = "yok.jpg";
        private string _adminLocation;

        public AddItemPage(string adminLocation)
        {
            InitializeComponent();
            _adminLocation = adminLocation;
            InstitutionPicker.SelectedItem = _adminLocation;
            InstitutionPicker.IsEnabled = false;
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Fotoğraf Ekle", "Vazgeç", null, "Kamerayı Aç", "Galeriden Seç");
            FileResult result = null;

            try
            {
                if (action == "Kamerayı Aç") result = await MediaPicker.Default.CapturePhotoAsync();
                else if (action == "Galeriden Seç") result = await MediaPicker.Default.PickPhotoAsync();
                else return;

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    SelectedImage.Source = ImageSource.FromStream(() => stream);

                    var uploadStream = await result.OpenReadAsync();
                    var content = new MultipartFormDataContent();
                    var streamContent = new StreamContent(uploadStream);

                    string fileName = string.IsNullOrWhiteSpace(result.FileName) ? "image.jpg" : result.FileName;
                    content.Add(streamContent, "file", fileName);

                    HttpClient client = new HttpClient();
                    // Link localhost olarak güncellendi
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
            catch (Exception ex) { await DisplayAlert("Hata", ex.Message, "Tamam"); }
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            var newItem = new
            {
                title = TitleEntry.Text,
                category = _adminLocation,
                location = LocationEntry.Text,
                finderName = FinderEntry.Text,
                secretAnswer = SecretFeaturesEntry.Text,
                imageUrl = _uploadedFileName,
                isHandedOver = false
            };

            try
            {
                HttpClient client = new HttpClient();
                var json = JsonSerializer.Serialize(newItem);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                // Link localhost olarak güncellendi
                var response = await client.PostAsync("http://localhost:5280/api/Items", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Başarılı", "Kaydedildi!", "Tamam");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex) { await DisplayAlert("Hata", ex.Message, "Tamam"); }
        }
    }
}