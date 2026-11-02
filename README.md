# Hesap Makinesi – BSM352 Görsel Programlama Ödev 1

**Öğrenci:** [Emine Özüdoğru] – [22010310062]

 **Video:** https://ogrencibartinedu-my.sharepoint.com/:v:/g/personal/22010310062_ogrenci_bartin_edu_tr/IQAluJ-09RBUSrpI7phAyrjiAdm1Oo51oA7-uySe0uijjfM?nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJPbmVEcml2ZUZvckJ1c2luZXNzIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXciLCJyZWZlcnJhbFZpZXciOiJNeUZpbGVzTGlua0NvcHkifX0&e=adl7kX

Windows Hesap Makinesi'nin Standart ve Bilimsel modları ile tema seçilebilen Ayarlar sayfası, .NET MAUI (.NET 10) ve XAML ile geliştirilmiştir.

## Özellikler
- Standart mod: dört işlem, yüzde, kare, karekök, 1/x, hafıza (MC, MR, M+, M−, MS)
- Bilimsel mod: işlem önceliği, parantez, xʸ, kök, log, ln, sin/cos/tan (DEG/RAD/GRAD), n!, π, e
- Shell (FlyoutItem) ile Standart / Bilimsel / Ayarlar geçişi
- Açık / Koyu / Sistem teması: UserAppTheme + AppThemeBinding, Preferences ile kalıcı
- Stiller: Colors.xaml + HesapStilleri.xaml (örtük stil, x:Key ve BasedOn ile türetilmiş açık stiller)
- Hata yönetimi: sıfıra bölme, negatif karekök, tanımsız logaritma/tanjant, taşma

## Çalıştırma
Visual Studio 2026 → HesapMakinesi.slnx → hedef: Windows Machine → F5
