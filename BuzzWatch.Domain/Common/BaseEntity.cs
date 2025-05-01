namespace BuzzWatch.Domain.Common
{
    public abstract class BaseEntity<TKey>
    {
        public TKey Id { get; protected set; } = default!;
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void AddDomainEvent(IDomainEvent evt) => _domainEvents.Add(evt);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
} 