using Sarah.API.BusinessObjects;

namespace Sarah.Persons.WebApi.Services;

public interface INamedPositionService
{
    Task<string> ResolveNamedPositionAsync(LocatorPosition? position, CancellationToken cancellationToken = default);
}
