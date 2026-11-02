using System.Globalization;

namespace HesapMakinesi;

/// <summary>Trigonometrik işlemlerde kullanılan açı birimi.</summary>
public enum AciBirimi { Derece, Radyan, Grad }

/// <summary>Kullanıcıya gösterilecek hesaplama hataları (sıfıra bölme vb.).</summary>
public class HesapHatasi(string mesaj) : Exception(mesaj);

/// <summary>
/// Hesap makinesinin tüm hesaplama ve durum mantığı. Arayüzden bağımsızdır:
/// sayfalar yalnızca bu sınıfın metotlarını çağırır, Ekran ve Ifade değerlerini gösterir.
/// </summary>
public class HesapMotoru
{
    static readonly CultureInfo Tr = new("tr-TR");                  // ondalık ayırıcı: virgül
    static readonly CultureInfo Sabit = CultureInfo.InvariantCulture;
    const int MaksimumBasamak = 16;

    /// <summary>Son basılan tuşun türü: bir sonraki tuşun nasıl davranacağını belirler.</summary>
    enum SonTus { Bos, Sayi, Operator, Kapanis, Esittir }

    readonly bool islemOnceligi;       // Bilimsel: true (× ÷ önce) · Standart: false (soldan sağa)
    readonly List<string> tokenler = []; // Bekleyen ifade: sayılar, operatörler, parantezler
    string giris = "0";                // Ekrandaki sayı
    bool yeniSayi = true;              // true ise sonraki rakam yeni bir sayı başlatır
    SonTus son = SonTus.Bos;
    string hataMetni = "";
    string? tekrarIslem;               // "= =" tekrarı için son işlem (ör. 2 + 3 = = → 8)
    double tekrarSayi;

    public HesapMotoru(bool islemOnceligi) => this.islemOnceligi = islemOnceligi;

    // ==================== Arayüzün okuduğu bilgiler ====================
    public string Ekran => HataVar ? hataMetni : giris;
    public string Ifade { get; private set; } = "";
    public bool HataVar { get; private set; }
    public string? SonHata { get; private set; }      // Bu tuşta oluşan hata (uyarı göstermek için)
    public int AcikParantez { get; private set; }
    public double Hafiza { get; private set; }
    public bool HafizaVar { get; private set; }
    public AciBirimi Aci { get; private set; } = AciBirimi.Derece;
    public string AciMetni => Aci switch { AciBirimi.Radyan => "RAD", AciBirimi.Grad => "GRAD", _ => "DEG" };
    public bool BilimselGosterim { get; private set; }

    // ==================== Tuş komutları ====================

    /// <summary>Rakam (0–9) veya virgül ekler.</summary>
    public void RakamEkle(string r) => Calistir(() =>
    {
        if (yeniSayi) { YeniSayiyaBasla(r == "," ? "0," : r); return; }

        var parca = giris.Split('e');
        if (r == ",")
        {
            if (giris.Contains(',') || parca.Length > 1) return;           // tek virgül
        }
        else if (parca.Length > 1 ? parca[1].Count(char.IsDigit) >= 3      // üs en fazla 3 basamak
                                  : parca[0].Count(char.IsDigit) >= MaksimumBasamak) return;

        giris = giris switch
        {
            "0" when r != "," => r,
            "-0" when r != "," => "-" + r,
            _ => giris + r
        };
    }, hataSonrasiDevam: true);

    /// <summary>İkili işlem ekler: + - * / mod ^</summary>
    public void IslemEkle(string op) => Calistir(() =>
    {
        if (son == SonTus.Operator && tokenler.Count > 0 && Ikili(tokenler[^1]))
            tokenler[^1] = op;                       // art arda operatör: sonuncuyu değiştir
        else if (son == SonTus.Kapanis)
            tokenler.Add(op);
        else
        {
            if (son == SonTus.Esittir) tokenler.Clear();   // sonuçla devam et
            tokenler.Add(SayiTokeni(GirisDegeri()));
            tokenler.Add(op);
        }
        son = SonTus.Operator;
        yeniSayi = true;

        // Standart mod: Windows gibi ara sonucu hemen göster (5 + 3 × → 8)
        if (!islemOnceligi && AcikParantez == 0)
            giris = Formatla(Hesapla(tokenler.GetRange(0, tokenler.Count - 1)));
    });

    /// <summary>Eşittir: bekleyen ifadeyi hesaplar.</summary>
    public void Esittir() => Calistir(() =>
    {
        if (son == SonTus.Esittir)                  // "=" tekrarı: son işlemi tekrarla
        {
            if (tekrarIslem is null) return;
            var liste = new List<string> { SayiTokeni(GirisDegeri()), tekrarIslem, SayiTokeni(tekrarSayi) };
            Ifade = IfadeYaz(liste) + " =";
            giris = Formatla(Hesapla(liste));
            yeniSayi = true;
            return;
        }

        if (son != SonTus.Kapanis) tokenler.Add(SayiTokeni(GirisDegeri()));
        while (AcikParantez > 0) { tokenler.Add(")"); AcikParantez--; }   // kapanmamış parantezleri kapat

        tekrarIslem = null;
        if (tokenler.Count >= 3 && Ikili(tokenler[^2]) && !tokenler.Contains("("))
        {
            tekrarIslem = tokenler[^2];
            tekrarSayi = double.Parse(tokenler[^1], Sabit);
        }

        Ifade = IfadeYaz(tokenler) + " =";
        giris = Formatla(Hesapla(tokenler));        // hata olursa burada HesapHatasi fırlar
        tokenler.Clear();
        son = SonTus.Esittir;
        yeniSayi = true;
    });

    /// <summary>Tek sayıya uygulanan işlemler: kare, karekök, sin, log, n! ...</summary>
    public void TekliIslem(string ad) => Calistir(() =>
    {
        double x = MevcutDeger();
        giris = Formatla(TekliHesapla(ad, x));
        SonucuYerlestir();
    });

    /// <summary>Yüzde: a + b% → a'nın yüzde b'si · a × b% → b / 100</summary>
    public void Yuzde() => Calistir(() =>
    {
        double x = MevcutDeger();
        if (son == SonTus.Esittir) tokenler.Clear();
        bool toplamaCikarma = tokenler.Count >= 2 && (tokenler[^1] == "+" || tokenler[^1] == "-");
        double sonuc = toplamaCikarma
            ? Hesapla(tokenler.GetRange(0, tokenler.Count - 1)) * x / 100
            : x / 100;
        giris = Formatla(sonuc);
        SonucuYerlestir();
    });

    /// <summary>+/- tuşu: sayının (veya yazılan üssün) işaretini değiştirir.</summary>
    public void IsaretDegistir() => Calistir(() =>
    {
        bool yaziliyor = son == SonTus.Sayi && !yeniSayi;
        if (yaziliyor && giris.Contains('e'))          // 2e+3 → 2e-3
        {
            giris = giris.Contains("e+") ? giris.Replace("e+", "e-") : giris.Replace("e-", "e+");
            return;
        }
        if (yaziliyor)
        {
            if (giris != "0") giris = giris.StartsWith('-') ? giris[1..] : "-" + giris;
            return;
        }
        giris = Formatla(-MevcutDeger());
        SonucuYerlestir();
    });

    /// <summary>π veya e sabitini ekrana yazar.</summary>
    public void SabitEkle(string ad) => Calistir(() => SayiyiYerlestir(ad == "pi" ? Math.PI : Math.E),
                                             hataSonrasiDevam: true);

    /// <summary>exp tuşu: bilimsel üs girişi (2 exp 3 = 2000).</summary>
    public void UsEkle() => Calistir(() =>
    {
        if (son != SonTus.Sayi || yeniSayi) YeniSayiyaBasla("0");
        if (!giris.Contains('e')) giris += "e+";
    }, hataSonrasiDevam: true);

    public void ParantezAc() => Calistir(() =>
    {
        if (son == SonTus.Esittir) TumunuSifirla();
        if (son == SonTus.Sayi) { tokenler.Add(SayiTokeni(GirisDegeri())); tokenler.Add("*"); } // 2( → 2×(
        else if (son == SonTus.Kapanis) tokenler.Add("*");                                       // )( → )×(
        tokenler.Add("(");
        AcikParantez++;
        son = SonTus.Operator;
        yeniSayi = true;
    }, hataSonrasiDevam: true);

    public void ParantezKapat() => Calistir(() =>
    {
        if (AcikParantez == 0) return;
        if (son != SonTus.Kapanis) tokenler.Add(SayiTokeni(GirisDegeri()));
        tokenler.Add(")");
        AcikParantez--;
        giris = Formatla(GrupDegeri(tokenler.Count - 1, out _));   // parantezin değerini göster
        son = SonTus.Kapanis;
        yeniSayi = true;
    });

    /// <summary>MC, MR, M+, M-, MS</summary>
    public void HafizaIslemi(string kod) => Calistir(() =>
    {
        switch (kod)
        {
            case "MC": Hafiza = 0; HafizaVar = false; break;
            case "MR": if (HafizaVar) SayiyiYerlestir(Hafiza); break;
            case "M+": Hafiza = Kontrol(Hafiza + GirisDegeri()); HafizaVar = true; yeniSayi = true; break;
            case "M-": Hafiza = Kontrol(Hafiza - GirisDegeri()); HafizaVar = true; yeniSayi = true; break;
            case "MS": Hafiza = GirisDegeri(); HafizaVar = true; yeniSayi = true; break;
        }
    }, hataSonrasiDevam: kod == "MR");

    /// <summary>C: her şeyi temizler (hafıza hariç).</summary>
    public void Temizle() { SonHata = null; TumunuSifirla(); }

    /// <summary>CE: yalnızca ekrandaki sayıyı temizler.</summary>
    public void GirisiTemizle() => Calistir(() =>
    {
        if (son is SonTus.Esittir or SonTus.Kapanis) { TumunuSifirla(); return; }
        giris = "0";
        yeniSayi = true;
        if (son == SonTus.Sayi) son = tokenler.Count == 0 ? SonTus.Bos : SonTus.Operator;
    });

    /// <summary>⌫: yazılan sayının son karakterini siler.</summary>
    public void GeriSil() => Calistir(() =>
    {
        if (son == SonTus.Esittir) { Ifade = ""; return; }
        if (son != SonTus.Sayi || yeniSayi) return;
        giris = giris.EndsWith("e+") || giris.EndsWith("e-") ? giris[..^2] : giris[..^1];
        if (giris is "" or "-") giris = "0";
    });

    /// <summary>DEG → RAD → GRAD</summary>
    public void AciDegistir() => Aci = Aci switch
    {
        AciBirimi.Derece => AciBirimi.Radyan,
        AciBirimi.Radyan => AciBirimi.Grad,
        _ => AciBirimi.Derece
    };

    /// <summary>F-E: bilimsel gösterimi (1,2e+3) açar/kapatır.</summary>
    public void GosterimDegistir()
    {
        BilimselGosterim = !BilimselGosterim;
        if (HataVar || !yeniSayi) return;
        try { giris = Formatla(GirisDegeri()); } catch (HesapHatasi) { }
    }

    /// <summary>Hata durumuna geçer: ekranda mesaj gösterilir, sonraki tuş makineyi sıfırlar.</summary>
    public void HataBildir(string mesaj)
    {
        HataVar = true;
        hataMetni = mesaj;
        SonHata = mesaj;
        tokenler.Clear();
        AcikParantez = 0;
        giris = "0";
        yeniSayi = true;
        son = SonTus.Bos;
        tekrarIslem = null;
    }

    // ==================== İç yardımcılar ====================

    /// <summary>Her komutu güvenli çalıştırır: hata olursa uygulama çökmez, hata durumuna geçer.</summary>
    void Calistir(Action islem, bool hataSonrasiDevam = false)
    {
        SonHata = null;
        if (HataVar)
        {
            TumunuSifirla();                       // hatadan sonraki ilk tuş makineyi sıfırlar
            if (!hataSonrasiDevam) return;
        }
        try
        {
            islem();
            if (son != SonTus.Esittir) Ifade = IfadeYaz(tokenler);
        }
        catch (HesapHatasi h) { HataBildir(h.Message); }
        catch (Exception) { HataBildir("Geçersiz giriş"); }
    }

    void TumunuSifirla()
    {
        tokenler.Clear();
        giris = "0";
        yeniSayi = true;
        son = SonTus.Bos;
        AcikParantez = 0;
        HataVar = false;
        hataMetni = "";
        tekrarIslem = null;
        Ifade = "";
    }

    void YeniSayiyaBasla(string ilk)
    {
        if (son == SonTus.Esittir) TumunuSifirla();
        else if (son == SonTus.Kapanis) tokenler.Add("*");          // (2)3 → (2)×3
        giris = ilk;
        yeniSayi = false;
        son = SonTus.Sayi;
    }

    void SayiyiYerlestir(double deger)
    {
        if (son == SonTus.Esittir) TumunuSifirla();
        else if (son == SonTus.Kapanis) tokenler.Add("*");
        giris = Formatla(deger);
        yeniSayi = true;
        son = SonTus.Sayi;
    }

    void SonucuYerlestir()
    {
        if (son == SonTus.Esittir) tokenler.Clear();
        yeniSayi = true;
        son = SonTus.Sayi;
    }

    /// <summary>Ekrandaki değer; son tuş ")" ise parantez grubunu tek sayıya çevirir.</summary>
    double MevcutDeger()
    {
        if (son != SonTus.Kapanis) return GirisDegeri();
        int kapanis = tokenler.Count - 1;
        double deger = GrupDegeri(kapanis, out int acilis);
        tokenler.RemoveRange(acilis, kapanis - acilis + 1);
        return deger;
    }

    double GirisDegeri()
    {
        var metin = giris;
        if (metin.EndsWith("e+") || metin.EndsWith("e-")) metin += "0";
        if (!double.TryParse(metin, NumberStyles.Float, Tr, out var deger))
            throw new HesapHatasi("Geçersiz giriş");
        return deger;
    }

    double GrupDegeri(int kapanis, out int acilis)
    {
        int derinlik = 0;
        for (acilis = kapanis; acilis >= 0; acilis--)
        {
            if (tokenler[acilis] == ")") derinlik++;
            else if (tokenler[acilis] == "(" && --derinlik == 0) break;
        }
        if (acilis < 0) throw new HesapHatasi("Geçersiz ifade");
        return Hesapla(tokenler.GetRange(acilis + 1, kapanis - acilis - 1));
    }

    /// <summary>İfadeyi hesaplar (Shunting-yard): öncelik ve parantezleri dikkate alır.</summary>
    double Hesapla(List<string> liste)
    {
        if (liste.Count == 0) return 0;

        // 1) Ters Lehçe gösterime (RPN) çevir
        var cikis = new List<string>();
        var yigin = new Stack<string>();
        foreach (var t in liste)
        {
            if (t == "(") yigin.Push(t);
            else if (t == ")")
            {
                while (yigin.Count > 0 && yigin.Peek() != "(") cikis.Add(yigin.Pop());
                if (yigin.Count > 0) yigin.Pop();
            }
            else if (Ikili(t))
            {
                while (yigin.Count > 0 && Ikili(yigin.Peek()) &&
                       (Oncelik(yigin.Peek()) > Oncelik(t) ||
                        (Oncelik(yigin.Peek()) == Oncelik(t) && t != "^")))
                    cikis.Add(yigin.Pop());
                yigin.Push(t);
            }
            else cikis.Add(t);
        }
        while (yigin.Count > 0)
        {
            var t = yigin.Pop();
            if (t != "(") cikis.Add(t);
        }

        // 2) RPN'i hesapla
        var sayilar = new Stack<double>();
        foreach (var t in cikis)
        {
            if (Ikili(t))
            {
                if (sayilar.Count < 2) throw new HesapHatasi("Geçersiz ifade");
                double b = sayilar.Pop(), a = sayilar.Pop();
                sayilar.Push(IkiliHesapla(t, a, b));
            }
            else sayilar.Push(double.Parse(t, Sabit));
        }
        if (sayilar.Count != 1) throw new HesapHatasi("Geçersiz ifade");
        return Kontrol(sayilar.Pop());
    }

    int Oncelik(string op) => !islemOnceligi ? 1 : op switch
    {
        "+" or "-" => 1,
        "*" or "/" or "mod" => 2,
        _ => 3                                                  // ^
    };

    static bool Ikili(string t) => t is "+" or "-" or "*" or "/" or "mod" or "^";

    static double IkiliHesapla(string op, double a, double b) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => b == 0 ? throw new HesapHatasi("Sıfıra bölünemez") : a / b,
        "mod" => b == 0 ? throw new HesapHatasi("Sıfıra bölünemez") : a % b,
        "^" => Math.Pow(a, b),
        _ => throw new HesapHatasi("Geçersiz işlem")
    };

    double TekliHesapla(string ad, double x) => Kontrol(ad switch
    {
        "tersi" => x == 0 ? throw new HesapHatasi("Sıfıra bölünemez") : 1 / x,
        "kare" => x * x,
        "kup" => x * x * x,
        "karekok" => x < 0 ? throw new HesapHatasi("Negatif sayının karekökü alınamaz") : Math.Sqrt(x),
        "kupkok" => Math.Cbrt(x),
        "mutlak" => Math.Abs(x),
        "faktoriyel" => Faktoriyel(x),
        "on_us" => Math.Pow(10, x),
        "iki_us" => Math.Pow(2, x),
        "e_us" => Math.Exp(x),
        "log" => x <= 0 ? throw new HesapHatasi("Logaritma yalnız pozitif sayılar için tanımlıdır") : Math.Log10(x),
        "ln" => x <= 0 ? throw new HesapHatasi("Logaritma yalnız pozitif sayılar için tanımlıdır") : Math.Log(x),
        "taban" => Math.Floor(x),
        "tavan" => Math.Ceiling(x),
        "sin" => Yuvarla(Math.Sin(Radyana(x))),
        "cos" => Yuvarla(Math.Cos(Radyana(x))),
        "tan" => Tanjant(x),
        "asin" => TersTrig(Math.Asin, x),
        "acos" => TersTrig(Math.Acos, x),
        "atan" => Yuvarla(AciyaCevir(Math.Atan(x))),
        _ => throw new HesapHatasi("Bilinmeyen işlem")
    });

    static double Faktoriyel(double x)
    {
        if (x < 0 || x != Math.Floor(x))
            throw new HesapHatasi("Faktöriyel yalnız pozitif tam sayılar için tanımlıdır");
        if (x > 170) throw new HesapHatasi("Taşma: sonuç çok büyük");
        double sonuc = 1;
        for (int i = 2; i <= (int)x; i++) sonuc *= i;
        return sonuc;
    }

    double Tanjant(double x)
    {
        double r = Radyana(x);
        if (Math.Abs(Math.Cos(r)) < 1e-12) throw new HesapHatasi("Geçersiz giriş: tan bu açıda tanımsız");
        return Yuvarla(Math.Tan(r));
    }

    double TersTrig(Func<double, double> f, double x)
    {
        if (x < -1 || x > 1) throw new HesapHatasi("Geçersiz giriş: değer -1 ile 1 arasında olmalı");
        return Yuvarla(AciyaCevir(f(x)));
    }

    double Radyana(double x) => Aci switch
    {
        AciBirimi.Derece => x * Math.PI / 180,
        AciBirimi.Grad => x * Math.PI / 200,
        _ => x
    };

    double AciyaCevir(double r) => Aci switch
    {
        AciBirimi.Derece => r * 180 / Math.PI,
        AciBirimi.Grad => r * 200 / Math.PI,
        _ => r
    };

    static double Yuvarla(double r) => Math.Abs(r) < 1e-15 ? 0 : r;   // sin(180°) = 0 olsun

    static double Kontrol(double v)
    {
        if (double.IsNaN(v)) throw new HesapHatasi("Geçersiz giriş");
        if (double.IsInfinity(v)) throw new HesapHatasi("Taşma: sonuç çok büyük");
        return v;
    }

    static string SayiTokeni(double v) => Kontrol(v).ToString("R", Sabit);

    string Formatla(double v)
    {
        Kontrol(v);
        if (v == 0) return "0";
        string s = BilimselGosterim ? v.ToString("0.###############E+0", Tr) : v.ToString("G15", Tr);
        return s.Replace("E", "e");
    }

    string IfadeYaz(IEnumerable<string> liste) => string.Join(" ", liste.Select(t => t switch
    {
        "*" => "×",
        "/" => "÷",
        "-" => "−",
        "+" or "^" or "mod" or "(" or ")" => t,
        _ => Formatla(double.Parse(t, Sabit))
    }));
}