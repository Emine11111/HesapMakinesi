namespace HesapMakinesi;

/// <summary>
/// Standart mod sayfası. Hesaplamayı HesapMotoru yapar;
/// bu sınıf yalnızca tuş olaylarını motora iletir ve ekranı günceller.
/// </summary>
public partial class StandartPage : ContentPage
{
    // Standart modda Windows Hesap Makinesi gibi işlem önceliği yoktur (soldan sağa).
    readonly HesapMotoru motor = new(islemOnceligi: false);

    public StandartPage()
    {
        InitializeComponent();
        EkraniGuncelle();
    }

    // ---------- Tuş olayları (Clicked) ----------
    // Hangi tuşa basıldığını CommandParameter ile öğreniyoruz (WinForms'taki Tag gibi).
    void Rakam_Clicked(object? sender, EventArgs e) => Uygula(() => motor.RakamEkle(Parametre(sender)));
    void Islem_Clicked(object? sender, EventArgs e) => Uygula(() => motor.IslemEkle(Parametre(sender)));
    void Tekli_Clicked(object? sender, EventArgs e) => Uygula(() => motor.TekliIslem(Parametre(sender)));
    void Hafiza_Clicked(object? sender, EventArgs e) => Uygula(() => motor.HafizaIslemi(Parametre(sender)));
    void Yuzde_Clicked(object? sender, EventArgs e) => Uygula(motor.Yuzde);
    void Esittir_Clicked(object? sender, EventArgs e) => Uygula(motor.Esittir);
    void Isaret_Clicked(object? sender, EventArgs e) => Uygula(motor.IsaretDegistir);
    void Temizle_Clicked(object? sender, EventArgs e) => Uygula(motor.Temizle);
    void GirisiTemizle_Clicked(object? sender, EventArgs e) => Uygula(motor.GirisiTemizle);
    void GeriSil_Clicked(object? sender, EventArgs e) => Uygula(motor.GeriSil);

    /// <summary>Her tuşta: işlemi çalıştır → ekranı yenile → hata varsa kullanıcıyı uyar.</summary>
    async void Uygula(Action islem)
    {
        try { islem(); }
        catch (Exception ex) { motor.HataBildir(ex.Message); }   // güvenlik ağı: uygulama asla çökmez

        EkraniGuncelle();

        if (motor.SonHata is string mesaj)                      // ör. sıfıra bölme
            await DisplayAlertAsync("Hata", mesaj, "Tamam");
    }

    void EkraniGuncelle()
    {
        SonucLabel.Text = motor.Ekran;
        IfadeLabel.Text = motor.Ifade;

        // Uzun sayılarda küçük yazı: boyut sayfada değil, stil dosyasında tanımlı
        SonucLabel.Style = Stiller.Al(motor.Ekran.Length > 11 ? "EkranSonucKucuk" : "EkranSonuc") ?? SonucLabel.Style;

        // Hafızada sayı yoksa MC ve MR pasif (Disabled görsel durumu)
        McButon.IsEnabled = MrButon.IsEnabled = motor.HafizaVar;
    }

    static string Parametre(object? sender) => (sender as Button)?.CommandParameter?.ToString() ?? "";
}