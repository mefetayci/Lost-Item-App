namespace CampusLostAndFound.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // AppShell yerine sayfalar arası geçişi sağlayan NavigationPage kullanıyoruz
            return new Window(new NavigationPage(new MainPage()));
        }
    }
}