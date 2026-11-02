namespace HesapMakinesi;

/// <summary>Ayarlar: Açık / Koyu / Sistem teması seçimi.</summary>
public partial class AyarlarPage : ContentPage
{
    bool yukleniyor;   // Kayıtlı seçimi gösterirken olay tetiklenmesin

    public AyarlarPage()
    {
        InitializeComponent();
        KayitliSecimiGoster();
        SurumLabel.Text = $"Sürüm {AppInfo.Current.VersionString}";
    }

    /// <summary>Preferences'taki temayı ilgili RadioButton'da işaretler.</summary>
    void KayitliSecimiGoster()
    {
        yukleniyor = true;
        switch (TemaServisi.KayitliTema)
        {
            case TemaServisi.Acik: AcikRadio.IsChecked = true; break;
            case TemaServisi.Koyu: KoyuRadio.IsChecked = true; break;
            default: SistemRadio.IsChecked = true; break;
        }
        yukleniyor = false;
    }

    void Tema_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Seçimi bırakılan RadioButton da bu olayı tetikler → yalnız seçileni işle
        if (yukleniyor || !e.Value) return;

        if (sender is RadioButton rb && rb.Value is string tema)
            TemaServisi.Uygula(tema);   // tüm sayfalara anında yansır + kaydedilir
    }
}