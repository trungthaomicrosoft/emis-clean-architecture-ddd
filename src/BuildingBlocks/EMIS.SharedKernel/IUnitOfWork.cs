namespace EMIS.SharedKernel;

/// <summary>
/// Unit of Work pattern interface.
/// Maintains a list of objects affected by a business transaction
/// and coordinates the writing out of changes.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes and publishes domain events.
    /// </summary>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
