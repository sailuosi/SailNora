namespace SailNora.Foundation.Bootstrap;

public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
