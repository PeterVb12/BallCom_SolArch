namespace BallCom.Catalog.API.Messaging
{
    public interface IEventPublisher
    {
        void Publish<T>(T @event);
    }
}
