using System;
using System.IO;
using Newtonsoft.Json;
using DocuMind.Core.Enums;
using System.Collections.Generic;

namespace DocuMind.Infrastructure.Services
{
    public class AppSettings
    {
        // 1. YAPAY ZEKA ANAHTARLARI
        public Dictionary<AiProvider, string> ApiKeys { get; set; } = new Dictionary<AiProvider, string>();

        // 2. KİŞİLİK AYARI (SYSTEM PROMPT)
        public string SystemPrompt { get; set; } = "You are a helpful AI assistant. Answer based on the provided context.";

        // 3. SON KULLANILAN MODEL (Uygulama açılınca bunu seçecek)
        public AiProvider LastUsedProvider { get; set; } = AiProvider.Ollama;

        // 4. İNTERNET AJANI AYARLARI (Web Search)
        public string GoogleSearchApiKey { get; set; } = string.Empty;
        public string GoogleSearchEngineId { get; set; } = string.Empty;
    }

    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _currentSettings = new AppSettings();

        public SettingsService()
        {
            // AppData klasöründe güvenli bir yere kaydet
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocuMind");
            Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "settings.json");
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    _currentSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    // Dosya bozuksa sıfırla, uygulama çökmesin
                    _currentSettings = new AppSettings();
                }
            }
            else
            {
                _currentSettings = new AppSettings();
            }
        }

        // --- MODEL SEÇİMİNİ HATIRLA ---
        public void SaveLastProvider(AiProvider provider)
        {
            _currentSettings.LastUsedProvider = provider;
            SaveToFile();
        }

        public AiProvider GetLastProvider()
        {
            return _currentSettings.LastUsedProvider;
        }

        // --- İNTERNET ARAMA AYARLARI ---
        public void SaveWebSearchConfig(string apiKey, string engineId)
        {
            _currentSettings.GoogleSearchApiKey = apiKey;
            _currentSettings.GoogleSearchEngineId = engineId;
            SaveToFile();
        }

        public string GetWebSearchKey() => _currentSettings.GoogleSearchApiKey;
        public string GetSearchEngineId() => _currentSettings.GoogleSearchEngineId;

        // --- API KEY İŞLEMLERİ ---
        public void SaveApiKey(AiProvider provider, string key)
        {
            if (_currentSettings.ApiKeys.ContainsKey(provider))
                _currentSettings.ApiKeys[provider] = key;
            else
                _currentSettings.ApiKeys.Add(provider, key);

            SaveToFile();
        }

        public string GetApiKey(AiProvider provider)
        {
            return _currentSettings.ApiKeys.ContainsKey(provider) ? _currentSettings.ApiKeys[provider] : string.Empty;
        }

        // --- SYSTEM PROMPT İŞLEMLERİ ---
        public string GetSystemPrompt()
        {
            return _currentSettings.SystemPrompt;
        }

        public void SaveSystemPrompt(string prompt)
        {
            _currentSettings.SystemPrompt = prompt;
            SaveToFile();
        }

        // --- ORTAK KAYIT METODU ---
        private void SaveToFile()
        {
            try
            {
                File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(_currentSettings, Formatting.Indented));
            }
            catch (Exception)
            {
                // Diske yazma hatası (İzin yok vs.)
            }
        }
    }
}
