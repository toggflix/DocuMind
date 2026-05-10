using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocuMind.Core.Interfaces;

namespace DocuMind.Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        public async Task ExportChatAsTextAsync(string content, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        }
    }
}
