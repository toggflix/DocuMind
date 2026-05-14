using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuMind.Core.Interfaces;
using DocuMind.Core.Enums;
using DocuMind.Core.Models;
using DocuMind.Infrastructure.Services;
using Microsoft.Win32;
using System.Collections.Generic;

namespace DocuMind.UI.ViewModels
{
    // - CommunityToolkit otomatik üretimini engellemek için Nitelikleri (Attribute) kaldırdık.
    public class MainViewModel : ObservableObject
    {
        // --- SERVİSLER ---
        private readonly IPdfService _pdfService;
        private readonly SettingsService _settingsService;
        private readonly DatabaseService _dbService;
        private readonly IPromptService _promptService;
        private readonly IReportingService _reportingService;
        private readonly IWebSearchService _webSearchService;
        private readonly SemanticSearchService _semanticSearcher;

        // --- YAPAY ZEKA MOTORLARI ---
        private IAiService _currentAiService;
        private int _currentSessionId;

        // --- KOLEKSİYONLAR ---
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        public ObservableCollection<AiProvider> Providers { get; }
        public ObservableCollection<Session> ChatHistory { get; } = new ObservableCollection<Session>();
        public ObservableCollection<string> ModelVersions { get; } = new ObservableCollection<string>();
        public ObservableCollection<Persona> Personas { get; } = new ObservableCollection<Persona>();

        // --- MANUEL PROPERTYLER (Garantili Tanımlar) ---
        private string _userQuestion = string.Empty;
        public string UserQuestion { get => _userQuestion; set => SetProperty(ref _userQuestion, value); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _loadedFileName = "Hoş Geldiniz";
        public string LoadedFileName { get => _loadedFileName; set => SetProperty(ref _loadedFileName, value); }

        private bool _isFileLoaded;
        public bool IsFileLoaded { get => _isFileLoaded; set => SetProperty(ref _isFileLoaded, value); }

        private string _apiKeyInput = string.Empty;
        public string ApiKeyInput { get => _apiKeyInput; set => SetProperty(ref _apiKeyInput, value); }

        private string _apiConnectionStatus = string.Empty;
        public string ApiConnectionStatus { get => _apiConnectionStatus; set => SetProperty(ref _apiConnectionStatus, value); }

        private string _webSearchStatus = string.Empty;
        public string WebSearchStatus { get => _webSearchStatus; set => SetProperty(ref _webSearchStatus, value); }

        private bool _isApiKeyVisible;
        public bool IsApiKeyVisible { get => _isApiKeyVisible; set => SetProperty(ref _isApiKeyVisible, value); }

        private bool _isTemporaryChat;
        public bool IsTemporaryChat { get => _isTemporaryChat; set => SetProperty(ref _isTemporaryChat, value); }

        private bool _isWebSearchEnabled;
        public bool IsWebSearchEnabled { get => _isWebSearchEnabled; set => SetProperty(ref _isWebSearchEnabled, value); }

        private bool _requireCitations = true;
        public bool RequireCitations { get => _requireCitations; set => SetProperty(ref _requireCitations, value); }

        private string _googleApiKey = string.Empty;
        public string GoogleApiKey { get => _googleApiKey; set => SetProperty(ref _googleApiKey, value); }

        private string _googleSearchEngineId = string.Empty;
        public string GoogleSearchEngineId { get => _googleSearchEngineId; set => SetProperty(ref _googleSearchEngineId, value); }

        private string _searchQuery = string.Empty;
        public string SearchQuery { get => _searchQuery; set { if (SetProperty(ref _searchQuery, value)) FilterHistoryAsyncSafe(value); } }

        private void FilterHistoryAsyncSafe(string query)
        {
            _ = FilterHistoryAsync(query);
        }

        private AiProvider _selectedProvider;
        public AiProvider SelectedProvider { get => _selectedProvider; set { if (SetProperty(ref _selectedProvider, value)) OnSelectedProviderChanged(value); } }

        private string _selectedModelVersion = string.Empty;
        public string SelectedModelVersion { get => _selectedModelVersion; set { if (SetProperty(ref _selectedModelVersion, value)) CreateAiService(SelectedProvider); } }

        private Persona? _selectedPersona;
        public Persona? SelectedPersona { get => _selectedPersona; set => SetProperty(ref _selectedPersona, value); }

        private Session? _selectedHistoryItem;
        public Session? SelectedHistoryItem { get => _selectedHistoryItem; set { if (SetProperty(ref _selectedHistoryItem, value) && value != null) LoadSessionMessages(value.Id); } }

        // --- MANUEL KOMUTLAR (XAML Tarafının Beklediği Tam Liste) ---
        public IAsyncRelayCommand UploadFileCommand { get; }
        public IAsyncRelayCommand SendMessageCommand { get; }
        public IAsyncRelayCommand OpenPersonaEditorCommand { get; }
        public IAsyncRelayCommand ExportChatCommand { get; }
        public IAsyncRelayCommand GenerateBriefCommand { get; }
        public IAsyncRelayCommand<Session> DeleteSessionCommand { get; }
        public IAsyncRelayCommand<Persona> DeletePersonaCommand { get; }
        public IRelayCommand NewChatCommand { get; }
        public IRelayCommand ToggleTemporaryChatCommand { get; }
        public IRelayCommand SaveApiKeyCommand { get; }
        public IRelayCommand SaveGoogleSettingsCommand { get; }
        public IAsyncRelayCommand TestApiConnectionCommand { get; }
        public IAsyncRelayCommand TestWebSearchCommand { get; }

        // --- CONSTRUCTOR ---
        public MainViewModel(IPdfService pdfService, SettingsService settingsService, DatabaseService dbService,
                            IPromptService promptService, IReportingService reportingService,
                            IWebSearchService webSearchService, SemanticSearchService semanticSearcher)
        {
            _pdfService = pdfService;
            _settingsService = settingsService;
            _dbService = dbService;
            _promptService = promptService;
            _reportingService = reportingService;
            _webSearchService = webSearchService;
            _semanticSearcher = semanticSearcher;

            // Komutları manuel oluşturuyoruz (Belirsizlik hatalarını çözer)
            UploadFileCommand = new AsyncRelayCommand(UploadFileAsync);
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
            OpenPersonaEditorCommand = new AsyncRelayCommand(OpenPersonaEditorAsync);
            ExportChatCommand = new AsyncRelayCommand(ExportChatAsync);
            GenerateBriefCommand = new AsyncRelayCommand(GenerateBriefAsync);
            DeleteSessionCommand = new AsyncRelayCommand<Session>(DeleteSessionAsync);
            DeletePersonaCommand = new AsyncRelayCommand<Persona>(DeletePersonaAsync);
            NewChatCommand = new RelayCommand(StartNewChat);
            ToggleTemporaryChatCommand = new RelayCommand(() => IsTemporaryChat = !IsTemporaryChat);
            SaveApiKeyCommand = new RelayCommand(SaveApiKeyAction);
            SaveGoogleSettingsCommand = new RelayCommand(SaveGoogleSettingsAction);
            TestApiConnectionCommand = new AsyncRelayCommand(TestApiConnectionAsync);
            TestWebSearchCommand = new AsyncRelayCommand(TestWebSearchAsync);
            _currentAiService = new OllamaService();

            Providers = new ObservableCollection<AiProvider>(
                Enum.GetValues(typeof(AiProvider)).Cast<AiProvider>().Where(provider => provider != AiProvider.Claude));
            foreach (var p in _promptService.GetAvailablePersonas()) Personas.Add(p);
            SelectedPersona = Personas.FirstOrDefault() ?? _promptService.GetDefaultPersona();

            GoogleApiKey = _settingsService.GetWebSearchKey();
            GoogleSearchEngineId = _settingsService.GetSearchEngineId();
            IsWebSearchEnabled = _webSearchService.IsActive();
            WebSearchStatus = IsWebSearchEnabled ? "Web arama aktif." : "Web arama için Google API Key ve Search Engine ID gerekli.";

            SelectedProvider = _settingsService.GetLastProvider();
            _ = LoadHistoryAsync();
            if (SelectedProvider == AiProvider.Ollama) CheckOllamaHealth();
        }

        // --- METOTLAR ---

        private async void CheckOllamaHealth()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync("http://localhost:11434");
                if (!response.IsSuccessStatusCode)
                    Messages.Add(new Message { IsUser = false, Content = "**Uyari:** Ollama servisine erisilemiyor." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ollama saglik kontrolu hatasi: {ex.Message}");
            }
        }

        public async Task LoadHistoryAsync()
        {
            try
            {
                var sessions = await _dbService.GetSessionsAsync();
                Application.Current.Dispatcher.Invoke(() => {
                    ChatHistory.Clear();
                    foreach (var s in sessions) ChatHistory.Add(s);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistoryAsync hatası: {ex.Message}");
                Application.Current.Dispatcher.Invoke(() =>
                    Messages.Add(new Message { IsUser = false, Content = $"Geçmiş yüklenemedi: {ex.Message}" }));
            }
        }

        private async Task FilterHistoryAsync(string query)
        {
            try
            {
                var sessions = await _dbService.GetSessionsAsync();
                Application.Current.Dispatcher.Invoke(() => {
                    ChatHistory.Clear();
                    var normalizedQuery = query ?? string.Empty;
                    var filtered = sessions.Where(s =>
                        s.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                        || (s.Tags?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (s.Summary?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (s.KeyConcepts?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false));
                    foreach (var s in filtered) ChatHistory.Add(s);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterHistoryAsync hatası: {ex.Message}");
            }
        }

        private async void LoadSessionMessages(int sessionId)
        {
            IsLoading = true;
            try
            {
                var messages = await _dbService.GetMessagesBySessionIdAsync(sessionId);
                Application.Current.Dispatcher.Invoke(() => {
                    Messages.Clear();
                    foreach (var msg in messages) Messages.Add(msg);
                });
                _currentSessionId = sessionId;
                var session = ChatHistory.FirstOrDefault(s => s.Id == sessionId);
                if (session != null) LoadedFileName = session.Title;
                IsFileLoaded = true;
            }
            catch (Exception ex)
            {
                Messages.Add(new Message { IsUser = false, Content = $"Oturum yukleme hatasi: {ex.Message}" });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnSelectedProviderChanged(AiProvider value)
        {
            IsApiKeyVisible = value != AiProvider.Ollama;
            _settingsService.SaveLastProvider(value);
            ApiKeyInput = _settingsService.GetApiKey(value);
            ModelVersions.Clear();
            if (value == AiProvider.Gemini)
            {
                var installed = await new GeminiService(ApiKeyInput).GetAvailableModelsAsync();
                foreach (var m in installed) ModelVersions.Add(m);
            }
            else if (value == AiProvider.OpenAI) { ModelVersions.Add("gpt-4o"); ModelVersions.Add("gpt-3.5-turbo"); }
            else if (value == AiProvider.Ollama)
            {
                var installed = await new OllamaService().GetInstalledModelsAsync();
                foreach (var m in installed) ModelVersions.Add(m);
            }
            SelectedModelVersion = ModelVersions.FirstOrDefault() ?? string.Empty;
            CreateAiService(value);
        }

        private void CreateAiService(AiProvider provider)
        {
            string key = _settingsService.GetApiKey(provider);
            switch (provider)
            {
                case AiProvider.Gemini: _currentAiService = new GeminiService(key); break;
                case AiProvider.OpenAI: _currentAiService = new OpenAiService(key); break;
                case AiProvider.Ollama: _currentAiService = new OllamaService(); break;
            }
            _currentAiService.SetModel(SelectedModelVersion);
            _semanticSearcher.SetAiService(_currentAiService);
        }

        private string ExtractValue(string text, string key)
        {
            try
            {
                if (!text.Contains(key)) return "Belirlenemedi";
                var part = text.Split(new[] { key }, StringSplitOptions.None)[1];
                return part.Split('\n')[0].Trim().Replace("[", "").Replace("]", "");
            }
            catch { return "Analiz hatası"; }
        }

        // --- ASENKRON İŞLEM METOTLARI ---

        private async Task UploadFileAsync()
        {
            var dialog = new OpenFileDialog { Filter = "PDF Files|*.pdf" };
            if (dialog.ShowDialog() == true)
            {
                await LoadFileAsync(dialog.FileName);
            }
        }

        public async Task LoadFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            IsLoading = true;
            try
            {
                LoadedFileName = System.IO.Path.GetFileName(filePath);
                var pages = await Task.Run(() => _pdfService.ExtractPages(filePath));
                if (pages.Count == 0)
                {
                    Messages.Add(new Message { IsUser = false, Content = "PDF içeriği okunamadı." });
                    return;
                }

                var session = await _dbService.CreateSessionAsync(LoadedFileName, filePath);
                _currentSessionId = session.Id;
                await Task.Run(() => _semanticSearcher.IndexDocumentAsync(_currentSessionId, pages));
                _ = AnalyzeDocumentAsync(_currentSessionId, string.Join(" ", pages.Select(p => p.Text)));

                await LoadHistoryAsync();
                IsFileLoaded = true;
                Messages.Add(new Message { IsUser = false, Content = $"**{LoadedFileName}** başarıyla yüklendi. Analiz arka planda devam ediyor." });
            }
            catch (Exception ex)
            {
                Messages.Add(new Message { IsUser = false, Content = $"Dosya yükleme hatası: {ex.Message}" });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AnalyzeDocumentAsync(int sessionId, string fullText)
        {
            try
            {
                string prompt = @"Dökümanı analiz et. Şu formatta yanıt ver: 
                                ÖZET: [Kısa özet] 
                                KAVRAMLAR: [Anahtar kelimeler] 
                                TÜR: [Dosya türü]";
                var raw = await _currentAiService.GetResponseAsync(fullText.Substring(0, Math.Min(3000, fullText.Length)), prompt, "Belge Analiz Uzmanı");
                
                if (string.IsNullOrEmpty(raw) || raw.Contains("Hatası"))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        Messages.Add(new Message { IsUser = false, Content = $"⚠️ Dokument analiz başarısız oldu: {raw}" }));
                    System.Diagnostics.Debug.WriteLine($"Document analysis failed: {raw}");
                }
                else
                {
                    await _dbService.UpdateSessionAnalysisAsync(sessionId, ExtractValue(raw, "ÖZET:"), ExtractValue(raw, "KAVRAMLAR:"), ExtractValue(raw, "TÜR:"));
                    await LoadHistoryAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                        Messages.Add(new Message { IsUser = false, Content = "✅ Dokument analizi tamamlandı." }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Document analysis exception: {ex.Message}\n{ex.StackTrace}");
                Application.Current.Dispatcher.Invoke(() =>
                    Messages.Add(new Message { IsUser = false, Content = $"❌ Analiz hatası: {ex.Message}" }));
            }
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserQuestion)) return;
            string question = UserQuestion;
            UserQuestion = string.Empty;
            Messages.Add(new Message { IsUser = true, Content = question, Timestamp = DateTime.Now });
            IsLoading = true;

            try
            {
                string webContext = "";
                if (IsWebSearchEnabled && _webSearchService.IsActive())
                {
                    var webResults = await _webSearchService.SearchAsync(question);
                    webContext = webResults.Count > 0 ? "\n\nWEB BİLGİSİ:\n" + string.Join("\n", webResults) : "";
                }
                string pdfContext = _currentSessionId > 0 ? await _semanticSearcher.SearchRelevantContextAsync(_currentSessionId, question) : "";
                if (_currentSessionId > 0 && !IsTemporaryChat) await _dbService.SaveMessageAsync(_currentSessionId, true, question);

                string systemPrompt = BuildSystemPrompt();
                string response = await _currentAiService.GetResponseAsync($"DÖKÜMAN:\n{pdfContext}\n{webContext}", question, systemPrompt);

                Messages.Add(new Message { IsUser = false, Content = response, Timestamp = DateTime.Now });
                if (_currentSessionId > 0 && !IsTemporaryChat) await _dbService.SaveMessageAsync(_currentSessionId, false, response);
            }
            catch (Exception ex) { Messages.Add(new Message { IsUser = false, Content = $"Hata: {ex.Message}" }); }
            finally { IsLoading = false; }
        }

        private async Task GenerateBriefAsync()
        {
            if (_currentSessionId <= 0)
            {
                Messages.Add(new Message { IsUser = false, Content = "Brifing olusturmak icin once bir dokuman yukle veya gecmisten bir dokuman sec." });
                return;
            }

            IsLoading = true;
            try
            {
                const string briefQuestion = "yonetici ozeti kritik riskler aksiyon maddeleri karar noktalari takip sorulari";
                string context = await _semanticSearcher.SearchRelevantContextAsync(_currentSessionId, briefQuestion, 12);
                if (string.IsNullOrWhiteSpace(context))
                {
                    Messages.Add(new Message { IsUser = false, Content = "Bu dokuman icin yeterli aranabilir baglam bulunamadi. PDF metni taranabilir degilse OCR/embedding ayarlarini kontrol et." });
                    return;
                }

                string prompt = """
                Bu belgeyi profesyonel bir karar brifingine donustur.
                Markdown kullan ve su basliklari aynen uygula:
                1. Yönetici Özeti
                2. Kritik Bulgular
                3. Riskler ve Belirsizlikler
                4. Aksiyon Maddeleri
                5. Takip Soruları
                6. Sayfa Kanıtları

                Kurallar:
                - Her kritik iddiada [SAYFA n] biciminde kaynak goster.
                - Belgeden emin olmadigin noktayi varsayim gibi yazma; "belgede net degil" de.
                - Aksiyon maddelerini is fiiliyle baslat.
                - Kisa ama karar verdiren bir brifing yaz.
                """;

                var response = await _currentAiService.GetResponseAsync(context, prompt, BuildSystemPrompt());
                Messages.Add(new Message { IsUser = false, Content = response, Timestamp = DateTime.Now });
                if (!IsTemporaryChat)
                {
                    await _dbService.SaveMessageAsync(_currentSessionId, false, response);
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new Message { IsUser = false, Content = $"Brifing olusturma hatasi: {ex.Message}" });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string BuildSystemPrompt()
        {
            string systemPrompt = SelectedPersona?.SystemPrompt ?? _settingsService.GetSystemPrompt();
            if (!RequireCitations)
            {
                return systemPrompt;
            }

            return systemPrompt + "\n\nYanıtlarında mümkün olduğunda belge bağlamındaki [SAYFA n] etiketlerini kullan. Belgeden desteklenmeyen iddiaları açıkça belirt.";
        }

        private async Task OpenPersonaEditorAsync()
        {
            var win = new DocuMind.UI.Views.PersonaEditWindow { Owner = Application.Current.MainWindow };
            if (win.ShowDialog() == true && win.ResultPersona != null)
            {
                await _dbService.AddPersonaAsync(win.ResultPersona);
                Personas.Add(win.ResultPersona);
                SelectedPersona = win.ResultPersona;
            }
        }

        private async Task ExportChatAsync()
        {
            var dialog = new SaveFileDialog { Filter = "Markdown Dosyası|*.md|Metin Belgesi|*.txt", FileName = "Sohbet_Raporu.md" };
            if (dialog.ShowDialog() == true)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# {LoadedFileName} - Sohbet Raporu");
                sb.AppendLine($"*Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}*");
                sb.AppendLine("---\n");

                foreach (var m in Messages)
                {
                    string sender = m.IsUser ? "Kullanıcı" : "Yapay Zeka (" + SelectedProvider.ToString() + ")";
                    sb.AppendLine($"### 👤 {sender}");
                    sb.AppendLine();
                    sb.AppendLine(m.Content);
                    sb.AppendLine();
                    sb.AppendLine("---");
                }

                await _reportingService.ExportChatAsTextAsync(sb.ToString(), dialog.FileName);
                Messages.Add(new Message { IsUser = false, Content = $"Sohbet raporu başarıyla dışa aktarıldı: {dialog.FileName}" });
            }
        }

        private async Task DeleteSessionAsync(Session? session)
        {
            if (session != null && MessageBox.Show($"'{session.Title}' silinsin mi?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteSessionAsync(session.Id);
                ChatHistory.Remove(session);
                if (_currentSessionId == session.Id) StartNewChat();
            }
        }

        private async Task DeletePersonaAsync(Persona? persona)
        {
            if (persona == null) return;
            if (persona.IsDefault)
            {
                MessageBox.Show("Varsayilan uzman profilleri silinemez.");
                return;
            }

            if (MessageBox.Show($"'{persona.Name}' uzman profili silinsin mi?", "Onay", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            await _dbService.DeletePersonaAsync(persona.Id);
            Personas.Remove(persona);
            SelectedPersona = Personas.FirstOrDefault() ?? _promptService.GetDefaultPersona();
        }

        private void StartNewChat()
        {
            Messages.Clear();
            _currentSessionId = 0;
            IsFileLoaded = false;
            LoadedFileName = "Yeni sohbet";
            SelectedHistoryItem = null;
        }

        private void SaveApiKeyAction()
        {
            if (SelectedProvider == AiProvider.Ollama) return;

            _settingsService.SaveApiKey(SelectedProvider, ApiKeyInput);
            CreateAiService(SelectedProvider);
            ApiConnectionStatus = "API anahtarı kaydedildi.";
        }

        private void SaveGoogleSettingsAction()
        {
            _settingsService.SaveWebSearchConfig(GoogleApiKey, GoogleSearchEngineId);
            IsWebSearchEnabled = _webSearchService.IsActive();
            WebSearchStatus = IsWebSearchEnabled
                ? "Web arama ayarlari kaydedildi."
                : "Web arama için Google API Key ve Search Engine ID gerekli.";
        }

        private async Task TestApiConnectionAsync()
        {
            SaveApiKeyAction();
            ApiConnectionStatus = "Bağlantı test ediliyor...";

            try
            {
                if (SelectedProvider == AiProvider.Gemini)
                {
                    var gemini = new GeminiService(ApiKeyInput);
                    gemini.SetModel(SelectedModelVersion);
                    var response = await gemini.GetResponseAsync(
                        string.Empty,
                        "Sadece 'OK' yaz.",
                        "Kısa ve net cevap ver.");

                    ApiConnectionStatus = response.Contains("OK", StringComparison.OrdinalIgnoreCase)
                        ? "Gemini bağlantısı başarılı."
                        : response;
                    return;
                }

                var testResponse = await _currentAiService.GetResponseAsync(string.Empty, "Sadece 'OK' yaz.", "Kısa ve net cevap ver.");
                ApiConnectionStatus = testResponse.Contains("OK", StringComparison.OrdinalIgnoreCase)
                    ? $"{SelectedProvider} bağlantısı başarılı."
                    : testResponse;
            }
            catch (Exception ex)
            {
                ApiConnectionStatus = $"Bağlantı testi başarısız: {ex.Message}";
            }
        }

        private async Task TestWebSearchAsync()
        {
            SaveGoogleSettingsAction();
            if (!_webSearchService.IsActive())
            {
                WebSearchStatus = "Once Google API Key ve Search Engine ID gir.";
                return;
            }

            WebSearchStatus = "Web arama test ediliyor...";
            var results = await _webSearchService.SearchAsync("DocuMind PDF AI");
            WebSearchStatus = results.Count > 0
                ? $"Web arama calisiyor. {results.Count} sonuc alindi."
                : "Web arama sonuc dondurmedi. API key, Search Engine ID veya kota ayarlarini kontrol et.";
        }
    }
}
