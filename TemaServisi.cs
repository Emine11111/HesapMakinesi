namespace HesapMakinesi;


public static class TemaServisi
{
    const string Anahtar = "tema";               
    public const string Acik = "Acik";
    public const string Koyu = "Koyu";
    public const string Sistem = "Sistem";

   
    public static string KayitliTema => Preferences.Default.Get(Anahtar, Sistem);

    public static void Uygula(string tema, Application? uygulama = null)
    {
        var app = uygulama ?? Application.Current;
        if (app is null) return;

        app.UserAppTheme = tema switch
        {
            Acik => AppTheme.Light,
            Koyu => AppTheme.Dark,
            _ => AppTheme.Unspecified          
        };
        Preferences.Default.Set(Anahtar, tema);
    }
}