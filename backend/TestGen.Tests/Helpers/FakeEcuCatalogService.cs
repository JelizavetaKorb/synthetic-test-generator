using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Tests.Helpers;

internal sealed class FakeEcuCatalogService : IEcuCatalogService
{
    private readonly EcuCatalogEntry[] _entries;

    public FakeEcuCatalogService(params EcuCatalogEntry[] entries) => _entries = entries;

    public IReadOnlyList<EcuCatalogEntry> GetAll() => _entries;

    public EcuCatalogEntry? GetById(string ecuId) =>
        _entries.FirstOrDefault(e =>
            e.EcuId.Equals(ecuId, StringComparison.OrdinalIgnoreCase));
}
