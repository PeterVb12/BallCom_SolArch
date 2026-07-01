namespace BallCom.Warehouse.API.Messaging
{
    public interface IEventPublisher
    {
        void Publish<T>(T @event);
    }
}
