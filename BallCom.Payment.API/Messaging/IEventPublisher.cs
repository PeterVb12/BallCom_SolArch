namespace BallCom.Payment.API.Messaging
{
    public interface IEventPublisher
    {
        void Publish<T>(T @event);
    }
}
