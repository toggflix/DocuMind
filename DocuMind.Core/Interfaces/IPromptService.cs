using System.Collections.Generic;
using DocuMind.Core.Models;

namespace DocuMind.Core.Interfaces
{
    public interface IPromptService
    {
        List<Persona> GetAvailablePersonas();
        Persona GetDefaultPersona();
    }
}