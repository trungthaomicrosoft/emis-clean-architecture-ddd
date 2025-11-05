namespace EMIS.SharedKernel;

/// <summary>
/// Base interface for all repositories.
/// Repositories are responsible for retrieving and persisting aggregates.
/// </summary>
/// <typeparam name="T">The aggregate root type</typeparam>
public interface IRepository<T> where T : IAggregateRoot
{
    /// <summary>
    /// Unit of Work for managing transactions.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }
}
