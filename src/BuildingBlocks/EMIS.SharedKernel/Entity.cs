namespace EMIS.SharedKernel;

/// <summary>
/// Base class for all entities in the domain.
/// Entities have identity and are distinguished by their Id.
/// </summary>
public abstract class Entity
{
    private int? _requestedHashCode;
    private List<IDomainEvent>? _domainEvents;

    public virtual Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Domain events occurred.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent>? DomainEvents => _domainEvents?.AsReadOnly();

    /// <summary>
    /// Add a domain event to the entity.
    /// </summary>
    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents ??= new List<IDomainEvent>();
        _domainEvents.Add(eventItem);
    }

    /// <summary>
    /// Remove a domain event from the entity.
    /// </summary>
    public void RemoveDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents?.Remove(eventItem);
    }

    /// <summary>
    /// Clear all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    /// <summary>
    /// Check if this entity is transient (not yet persisted to database).
    /// </summary>
    public bool IsTransient()
    {
        return Id == Guid.Empty;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not Entity)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetType() != obj.GetType())
            return false;

        Entity item = (Entity)obj;

        if (item.IsTransient() || IsTransient())
            return false;

        return item.Id == Id;
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            if (!_requestedHashCode.HasValue)
                _requestedHashCode = Id.GetHashCode() ^ 31;

            return _requestedHashCode.Value;
        }
        else
            return base.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (Equals(left, null))
            return Equals(right, null);
        else
            return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
