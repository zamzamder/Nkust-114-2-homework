using System.Collections.Generic;

namespace Project1.Web.Services
{
    public interface ICnsMappingService
    {
        // Try get Unicode character mapped from a CNS11643 code string (e.g., "1-4E00" or similar)
        bool TryGetUnicode(string cnsCode, out string unicodeChar);

        // Get CNS codes mapped to a Unicode character
        IEnumerable<string> GetCnsCodes(string unicodeChar);

        // Ensure data loaded (synchronous)
        void EnsureLoaded();
    }
}