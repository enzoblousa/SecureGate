using SecureGate.Application.Abstractions;

namespace SecureGate.Tests.Auth;

public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = new();

    public IReadOnlyList<object> PublishedEvents => _publishedEvents;

    public Exception? ExceptionToThrow { get; set; }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }
}
