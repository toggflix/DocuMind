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

            // --- B. AI SERVICE FACTORY (NEW) ---
            services.AddSingleton<IAiServiceFactory, AiServiceFactory>();

            // --- C. PDF SERVİSLERİ ---
            services.AddSingleton<IPdfService, PdfPigService>();
            services.AddSingleton<SettingsService>();

            // --- D. SEMANTIC SEARCH (RAG MOTORU) ---
            services.AddSingleton<SemanticSearchService>(sp =>
            {
                var ai = sp.GetRequiredService<IAiServiceFactory>().CreateService(AiProvider.Ollama);
                var db = sp.GetRequiredService<AppDbContext>();
                return new SemanticSearchService(ai, db);
            });

            // --- E. DİĞER SERVİSLER ---
            services.AddSingleton<IPromptService, PromptService>();
            services.AddSingleton<IReportingService, ReportingService>();
            services.AddSingleton<IWebSearchService, WebSearchService>();

            // --- F. VIEWMODEL ---
            services.AddSingleton<MainViewModel>();

            // --- G. PENCERELER ---
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Database initialization asynchronously to prevent UI freeze
            InitializeDatabaseAsync();
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                using (var scope = Services.CreateScope())
                {
                    var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                    await databaseService.EnsureReadyAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Veritabanı başlatma hatası: {ex.Message}");
                MessageBox.Show($"Veritabanı başlatılırken hata oluştu: {ex.Message}\n\nUygulama kısıtlı olarak devam edecektir.", "Uyarı");
            }
        }
    }
}
