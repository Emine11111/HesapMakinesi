namespace HesapMakinesi;

public static class Stiller
{
    public static Style? Al(string anahtar) =>
        Application.Current?.Resources.TryGetValue(anahtar, out var deger) == true
            ? deger as Style
            : null;
}