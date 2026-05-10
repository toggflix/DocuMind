# DocuMind

DocuMind, .NET 8 WPF tabanli bir dokuman analiz ve AI destekli yardimci uygulamasidir. PDF metin cikarma, OCR, semantik arama ve farkli AI saglayicilarina baglanma kabiliyetleri icerir.

## Ozellikler

- WPF masaustu arayuzu
- PDF metin cikarma (PdfPig)
- OCR destegi (Tesseract)
- Semantik arama
- Coklu AI saglayici entegrasyonu:
  - OpenAI
  - Ollama
  - Gemini
- Raporlama ve ayar yonetimi

## Proje Yapisı

- `DocuMind.UI`: WPF arayuz
- `DocuMind.Core`: Domain modelleri, interface'ler ve cekirdek yardimci siniflar
- `DocuMind.Infrastructure`: Servisler, veri erisim ve dis bagimliliklar

## Gereksinimler

- .NET 8 SDK
- Windows (WPF icin)

## Kurulum ve Calistirma

1. Cozumu acin: `DocuMind.slnx`
2. NuGet paketlerini restore edin.
3. Uygulamayi calistirin:

```bash
dotnet run --project DocuMind.UI/DocuMind.UI.csproj
```

## Konfigurasyon

- API anahtari gerektiren saglayicilar (OpenAI/Gemini vb.) icin ilgili ayarlar `SettingsService` uzerinden yapilandirilmalidir.
- OCR icin `DocuMind.UI/tessdata` altinda dil dosyalari yer alir.
