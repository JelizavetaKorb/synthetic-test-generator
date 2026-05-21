using TestGen.Core.Models;

namespace TestGen.Core.Services;

public interface IEcuCatalogService
{
    IReadOnlyList<EcuCatalogEntry> GetAll();
    EcuCatalogEntry? GetById(string ecuId);
}
