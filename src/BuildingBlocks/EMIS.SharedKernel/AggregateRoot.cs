namespace EMIS.SharedKernel;

/// <summary>
/// Base class for Aggregate Roots in DDD.
/// An Aggregate is a cluster of domain objects that can be treated as a single unit.
/// </summary>
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    // Aggregate roots have the same functionality as entities
    // but serve as the entry point to the aggregate
    // and maintain transactional consistency boundary
}
