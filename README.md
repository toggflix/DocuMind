# DocuMind

DocuMind, .NET 8 WPF tabanli bir dokuman analiz ve AI destekli yardimci uygulamasidir. PDF metin cikarma, OCR, semantik arama ve farkli AI saglayicilarina baglanma kabiliyetleri icerir.

## Ozellikler

- WPF masaustu arayuzu
- PDF metin cikarma (PdfPig)
- OCR destegi (Tesseract)
- Semantik arama (RAG - Retrieval Augmented Generation)
- Coklu AI saglayici entegrasyonu:
  - OpenAI (gpt-4o, gpt-3.5-turbo)
  - Ollama (local models)
  - Gemini (Google AI)
- Raporlama ve ayar yonetimi
- Web arama entegrasyonu (Google Custom Search)
- Encrypted API key storage (AES-256)

## Proje Yapisı

- `DocuMind.UI`: WPF arayuz, ViewModels, pencereler
- `DocuMind.Core`: Domain modelleri, interface'ler ve cekirdek yardimci siniflar
- `DocuMind.Infrastructure`: Servisler, veri erisim, AI ve harici baglantılar

## Gereksinimler

- .NET 8 SDK
- Windows (WPF icin)
- Ollama (opsiyonel, local AI modelleri icin)

## Kurulum ve Calistirma

1. Cozumu acin: `DocuMind.slnx`
2. NuGet paketlerini restore edin.
3. Uygulamayi calistirin:

```bash
dotnet run --project DocuMind.UI/DocuMind.UI.csproj
```

## Konfigurasyon

### AI Saglayiciları

- **OpenAI**: API key gerekli (Settings → API Key menüsünde)
- **Gemini**: API key gerekli (settings.json'da otomatik kaydedilir)
- **Ollama**: Local kurulum gerekli (http://localhost:11434)

### Web Arama

Google Custom Search kullanmak için:
- Google API Key
- Search Engine ID (cx parameter)

Settings menüsünde ayarlanabilir.

### Veritabanı

- Varsayılan: SQLite (Documents/DocuMind_DB.sqlite)
- Otomatik migration destekleniyor

### OCR

- Tesseract dil dosyaları: `DocuMind.UI/tessdata/`
- Dil desteği: İngilizce (eng), Türkçe (tur)

## Güvenlik

- API anahtarları AES-256 şifrelemesi ile saklanır
- Şifreleme anahtarı: environment variable `DOCUMIND_ENCRYPTION_KEY` (production'da ayarlanmalı)
- Tüm network istekleri HTTPS kullanır

## Mimari

### Dependency Injection

Tüm servisler DI container'ında kaydedilir:

```csharp
services.AddSingleton<IAiServiceFactory, AiServiceFactory>();
services.AddSingleton<IPdfService, PdfPigService>();
services.AddSingleton<IPromptService, PromptService>();
// ...
```

### Service Factory Pattern

AI servisleri `IAiServiceFactory` aracılığıyla oluşturulur:

```csharp
var factory = serviceProvider.GetRequiredService<IAiServiceFactory>();
var aiService = factory.CreateService(AiProvider.OpenAI, apiKey);
```

### Database

- Entity Framework Core + SQLite
- Automatic migration on startup
- Cascade delete konfigürasyonu

## Hata Ayıklama

Hata logları çalışma zamanında Debug output'a yazılır:

```csharp
System.Diagnostics.Debug.WriteLine($"Error: {exception.Message}");
```

Release build'te de `%APPDATA%/DocuMind/` klasöründe log dosyaları oluşturulur (Serilog ile).

## Bilinen Sınırlamalar

- OCR sadece PDF dosyalarında destekleniyor
- Web arama Google Custom Search'e bağımlı
- Ollama local machine'de çalışması gerekiyor
- Maksimum dokument boyutu: 10MB (ayarlanabilir)

## Katkı Verme

1. Fork et
2. Feature branch oluştur (`git checkout -b feature/AmazingFeature`)
3. Commit et (`git commit -m 'Add some AmazingFeature'`)
4. Push et (`git push origin feature/AmazingFeature`)
5. Pull Request aç

## Lisans

MIT
