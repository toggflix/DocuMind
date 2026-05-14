using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using DocuMind.Core.Enums;
using System.Collections.Generic;

namespace DocuMind.Infrastructure.Services
{
    public class AppSettings
    {
        // 1. YAPAY ZEKA ANAHTARLARI (Şifreli saklanır)
        public Dictionary<AiProvider, string> ApiKeys { get; set; } = new Dictionary<AiProvider, string>();

        // 2. KİŞİLİK AYARI (SYSTEM PROMPT)
        public string SystemPrompt { get; set; } = "You are a helpful AI assistant. Answer based on the provided context.";

        // 3. SON KULLANILAN MODEL
        public AiProvider LastUsedProvider { get; set; } = AiProvider.Ollama;

        // 4. İNTERNET AJANI AYARLARI (Web Search)
        public string GoogleSearchApiKey { get; set; } = string.Empty;
        public string GoogleSearchEngineId { get; set; } = string.Empty;
    }

    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _currentSettings = new AppSettings();
        private const string ENCRYPTION_KEY = "DocuMind_Secure_Key_2024"; // Üretimde environment variable kullanın

        public SettingsService()
        {
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
                    
                    // Şifreli anahtarları decrypt et
                    foreach (var provider in _currentSettings.ApiKeys.Keys.ToList())
                    {
                        try
                        {
                            _currentSettings.ApiKeys[provider] = DecryptString(_currentSettings.ApiKeys[provider]);
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine($"API key decryption failed for {provider}");
                            _currentSettings.ApiKeys[provider] = string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Settings loading error: {ex.Message}");
                    _currentSettings = new AppSettings();
                }
            }
            else
            {
                _currentSettings = new AppSettings();
            }
        }

        private string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var iv = RandomNumberGenerator.GetBytes(aes.BlockSize / 8))
                    {
                        aes.IV = iv;
                        using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                        {
                            using (var ms = new MemoryStream())
                            {
                                ms.Write(iv, 0, iv.Length);
                                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                                {
                                    using (var sw = new StreamWriter(cs))
                                    {
                                        sw.Write(plainText);
                                    }
                                    return Convert.ToBase64String(ms.ToArray());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption error: {ex.Message}");
                return plainText; // Fallback: return unencrypted
            }
        }

        private string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var iv = new byte[aes.BlockSize / 8];
                    Array.Copy(buffer, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                        {
                            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                            {
                                using (var sr = new StreamReader(cs))
                                {
                                    return sr.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decryption error: {ex.Message}");
                return string.Empty;
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
            _currentSettings.GoogleSearchApiKey = EncryptString(apiKey);
            _currentSettings.GoogleSearchEngineId = engineId;
            SaveToFile();
        }

        public string GetWebSearchKey()
        {
            try
            {
                return DecryptString(_currentSettings.GoogleSearchApiKey);
            }
            catch
            {
                return _currentSettings.GoogleSearchApiKey; // Fallback to unencrypted if decryption fails
            }
        }

        public string GetSearchEngineId() => _currentSettings.GoogleSearchEngineId;

        // --- API KEY İŞLEMLERİ ---
        public void SaveApiKey(AiProvider provider, string key)
        {
            if (_currentSettings.ApiKeys.ContainsKey(provider))
                _currentSettings.ApiKeys[provider] = EncryptString(key);
            else
                _currentSettings.ApiKeys.Add(provider, EncryptString(key));

            SaveToFile();
        }

        public string GetApiKey(AiProvider provider)
        {
            if (!_currentSettings.ApiKeys.ContainsKey(provider))
                return string.Empty;

            try
            {
                return DecryptString(_currentSettings.ApiKeys[provider]);
            }
            catch
            {
                return _currentSettings.ApiKeys[provider]; // Fallback to unencrypted if decryption fails
            }
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
            }
        }
    }
}
