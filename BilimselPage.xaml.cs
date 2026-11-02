namespace HesapMakinesi;

/// <summary>
/// Bilimsel mod: işlem önceliği, parantez, üs, kök, logaritma, trigonometri, faktöriyel.
/// </summary>
public partial class BilimselPage : ContentPage
{
    // Bilimsel modda işlem önceliği vardır: 2 + 3 × 4 = 14
    readonly HesapMotoru motor = new(islemOnceligi: true);
    bool ikinciMod;   // "2nd" tuşu açık mı?

    public BilimselPage()
    {
        InitializeComponent();
        EkraniGuncelle();
    }

    // ---------- Tuş olayları ----------
    void Rakam_Clicked(object? sender, EventArgs e) => Uygula(() => motor.RakamEkle(Parametre(sender)));
    void Islem_Clicked(object? sender, EventArgs e) => Uygula(() => motor.IslemEkle(Parametre(sender)));
    void Tekli_Clicked(object? sender, EventArgs e) => Uygula(() => motor.TekliIslem(Parametre(sender)));
    void Hafiza_Clicked(object? sender, EventArgs e) => Uygula(() => motor.HafizaIslemi(Parametre(sender)));
    void Sabit_Clicked(object? sender, EventArgs e) => Uygula(() => motor.SabitEkle(Parametre(sender)));
    void Parantez_Clicked(object? sender, EventArgs e) => Uygula(() =>
    {
        if (Parametre(sender) == "(") motor.ParantezAc();
        else motor.ParantezKapat();
    });
    void Esittir_Clicked(object? sender, EventArgs e) => Uygula(motor.Esittir);
    void Isaret_Clicked(object? sender, EventArgs e) => Uygula(motor.IsaretDegistir);
    void Temizle_Clicked(object? sender, EventArgs e) => Uygula(motor.Temizle);
    void GeriSil_Clicked(object? sender, EventArgs e) => Uygula(motor.GeriSil);
    void Us_Clicked(object? sender, EventArgs e) => Uygula(motor.UsEkle);
    void Aci_Clicked(object? sender, EventArgs e) => Uygula(motor.AciDegistir);
    void Gosterim_Clicked(object? sender, EventArgs e) => Uygula(motor.GosterimDegistir);

    void Ikinci_Clicked(object? sender, EventArgs e)
    {
        ikinciMod = !ikinciMod;
        IkinciModuUygula();
    }

    /// <summary>2nd açıkken bazı tuşlar ters işlemlerine dönüşür (Windows'taki gibi).</summary>
    void IkinciModuUygula()
    {
        TusuAyarla(KareButon, ikinciMod ? "x³" : "x²", ikinciMod ? "kup" : "kare");
        TusuAyarla(KokButon, ikinciMod ? "³√x" : "²√x", ikinciMod ? "kupkok" : "karekok");
        TusuAyarla(OnUsButon, ikinciMod ? "2ˣ" : "10ˣ", ikinciMod ? "iki_us" : "on_us");
        TusuAyarla(LnButon, ikinciMod ? "eˣ" : "ln", ikinciMod ? "e_us" : "ln");
        TusuAyarla(SinButon, ikinciMod ? "sin⁻¹" : "sin", ikinciMod ? "asin" : "sin");
        TusuAyarla(CosButon, ikinciMod ? "cos⁻¹" : "cos", ikinciMod ? "acos" : "cos");
        TusuAyarla(TanButon, ikinciMod ? "tan⁻¹" : "tan", ikinciMod ? "atan" : "tan");

        // Aktifken vurgulu stil; renk sayfada değil stil dosyasında tanımlı
        IkinciButon.Style = Stiller.Al(ikinciMod ? "AktifTus" : "FonksiyonTus") ?? IkinciButon.Style;
    }

    static void TusuAyarla(Button tus, string metin, string islem)
    {
        tus.Text = metin;
        tus.CommandParameter = islem;
    }

    /// <summary>Her tuşta: işlemi çalıştır → ekranı yenile → hata varsa kullanıcıyı uyar.</summary>
    async void Uygula(Action islem)
    {
        try { islem(); }
        catch (Exception ex) { motor.HataBildir(ex.Message); }   // güvenlik ağı: uygulama asla çökmez

        EkraniGuncelle();

        if (motor.SonHata is string mesaj)
            await DisplayAlertAsync("Hata", mesaj, "Tamam");
    }

    void EkraniGuncelle()
    {
        SonucLabel.Text = motor.Ekran;
        IfadeLabel.Text = motor.Ifade;
        SonucLabel.Style = Stiller.Al(motor.Ekran.Length > 11 ? "EkranSonucKucuk" : "EkranSonuc") ?? SonucLabel.Style;

        McButon.IsEnabled = MrButon.IsEnabled = motor.HafizaVar;
        AciButon.Text = motor.AciMetni;
        FeButon.Style = Stiller.Al(motor.BilimselGosterim ? "AktifTus" : "SecenekTus") ?? FeButon.Style;

        // Açık parantez sayısını "(" tuşunda göster: (₂
        ParantezAcButon.Text = motor.AcikParantez > 0 ? "(" + AltSimge(motor.AcikParantez) : "(";
    }

    static string AltSimge(int sayi) =>
        string.Concat(sayi.ToString().Select(c => (char)('₀' + (c - '0'))));

    static string Parametre(object? sender) => (sender as Button)?.CommandParameter?.ToString() ?? "";
}