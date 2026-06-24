namespace BallCom.Ordering.API.Messaging
{
    public interface IEventPublisher
    {
        void Publish<T>(T @event);
    }
}
