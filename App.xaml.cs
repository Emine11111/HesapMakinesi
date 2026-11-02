namespace HesapMakinesi;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Uygulama açılırken son seçilen temayı (Preferences) uygula
        TemaServisi.Uygula(TemaServisi.KayitliTema, this);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var pencere = new Window(new AppShell()) { Title = "Hesap Makinesi" };

        // Masaüstünde Windows Hesap Makinesi'ne benzer pencere boyutu
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Desktop)
        {
            pencere.Width = 420;
            pencere.Height = 720;
            pencere.MinimumWidth = 340;
            pencere.MinimumHeight = 560;
        }
        return pencere;
    }
}