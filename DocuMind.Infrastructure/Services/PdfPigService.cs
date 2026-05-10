using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // ToArray için gerekli
using System.Text;
using DocuMind.Core.Interfaces;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DocuMind.Infrastructure.Services
{
    public class PdfPigService : IPdfService
    {
        private readonly string _tessDataPath;

        public PdfPigService()
        {
            _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        public List<(int PageNumber, string Text)> ExtractPages(string filePath)
        {
            var pages = new List<(int PageNumber, string Text)>();

            try
            {
                using (var pdf = PdfDocument.Open(filePath))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        StringBuilder pageText = new StringBuilder();
                        string rawText = page.Text;

                        if (!string.IsNullOrWhiteSpace(rawText))
                        {
                            pageText.Append(rawText);
                        }

                        // Metin azsa OCR devreye girer
                        if (string.IsNullOrWhiteSpace(rawText) || rawText.Length < 50)
                        {
                            var images = page.GetImages();
                            foreach (var image in images)
                            {
                                string ocrText = PerformOcrOnImage(image);
                                if (!string.IsNullOrWhiteSpace(ocrText))
                                {
                                    pageText.AppendLine("\n[OCR SONUCU]:");
                                    pageText.AppendLine(ocrText);
                                }
                            }
                        }

                        if (pageText.Length > 0)
                        {
                            pages.Add((page.Number, pageText.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF Hatası: {ex.Message}");
            }

            return pages;
        }

        private string PerformOcrOnImage(IPdfImage image)
        {
            try
            {
                byte[] imageBytes = image.RawBytes.ToArray();
                if (imageBytes == null || imageBytes.Length == 0) return string.Empty;

                using (var engine = new TesseractEngine(_tessDataPath, "tur+eng", EngineMode.Default))
                {
                    // Bitmap yerine direkt Memory'den Pix yüklüyoruz (Hata Çözümü)
                    using (var img = Pix.LoadFromMemory(imageBytes))
                    {
                        using (var page = engine.Process(img))
                        {
                            return page.GetText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR Hatası: {ex.Message}");
                return string.Empty;
            }
        }
    }
}