namespace Brer.Publisher.Interfaces
{
    public interface IBrerPublisher
    { 
        void Publish<T>(string topic, T obj); 
    }
}