namespace DocuMind.Core.Models
{
    public class Persona
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; //
        public string Description { get; set; } = string.Empty; //
        public string SystemPrompt { get; set; } = string.Empty; //
        public string IconKind { get; set; } = "Robot"; //
        public bool IsDefault { get; set; }
    }
}