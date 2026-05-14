namespace CampusLostAndFound.App
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string location = "";

            // Şimdilik manuel şifreler, ileride burayı API'den doğrulayacağız
            if (PasswordEntry.Text == "maltepe") location = "Maltepe Üniversitesi";
            else if (PasswordEntry.Text == "citys") location = "City's AVM";
            else if (PasswordEntry.Text == "marmaray") location = "Marmaray";

            if (!string.IsNullOrEmpty(location))
            {
                // Global (Statik) değişkenleri güncelleyerek sistemi Admin moduna geçiriyoruz
                MainPage.GlobalIsAdmin = true;
                MainPage.GlobalAdminLocation = location;

                await DisplayAlert("Başarılı", $"{location} görevlisi olarak giriş yapıldı.", "Tamam");

                // DEĞİŞEN KISIM: Artık eski sayfaya geri dönmüyoruz, yeni istatistik paneline yönlendiriyoruz.
                await Navigation.PushAsync(new AdminDashboardPage(location));
            }
            else
            {
                await DisplayAlert("Hata", "Hatalı şifre girdiniz!", "Tamam");
                PasswordEntry.Text = string.Empty;
            }
        }
    }
}