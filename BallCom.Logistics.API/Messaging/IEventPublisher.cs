namespace BallCom.Logistics.API.Messaging
{
    public interface IEventPublisher
    {
        void Publish<T>(T @event);
    }
}
