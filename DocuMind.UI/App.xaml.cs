using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DocuMind.Core.Interfaces;
using DocuMind.Infrastructure.Data;
using DocuMind.Infrastructure.Services;
using DocuMind.UI.ViewModels;
using DocuMind.UI.Views;

namespace DocuMind.UI
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // --- A. VERİTABANI (TEK SEFER KAYDEDİLMELİ) ---
            services.AddDbContext<AppDbContext>();
            services.AddSingleton<DatabaseService>();

            // --- B. AI VE PDF SERVİSLERİ ---
            services.AddSingleton<IPdfService, PdfPigService>();
            services.AddSingleton<SettingsService>();

            // IAiService için mevcut sağlayıcıyı SettingsService'e göre çözmek gerekebilir
            // Şimdilik varsayılan bir IAiService kaydı (Örn: Ollama veya Gemini) olmalı.
            // Eğer Ollama kullanıyorsan:
            services.AddSingleton<IAiService, OllamaService>();

            // --- C. SEMANTIC SEARCH (RAG MOTORU) ---
            // İşte senin aradığın, "db" parametresi hatasını çözen kısım burası:
            services.AddSingleton<SemanticSearchService>(sp =>
            {
                var ai = sp.GetRequiredService<IAiService>();
                var db = sp.GetRequiredService<AppDbContext>();
                return new SemanticSearchService(ai, db);
            });

            // --- D. DİĞER PRO SERVİSLER ---
            services.AddSingleton<IPromptService, PromptService>();
            services.AddSingleton<IReportingService, ReportingService>();
            services.AddSingleton<IWebSearchService, WebSearchService>();

            // --- E. VIEWMODEL ---
            services.AddSingleton<MainViewModel>();

            // --- F. PENCERELER ---
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                using (var scope = Services.CreateScope())
                {
                    var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                    databaseService.EnsureReadyAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı hatası: {ex.Message}");
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
