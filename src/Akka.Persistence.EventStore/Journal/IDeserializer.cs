using Akka.Serialization;
using EventStore.ClientAPI;

namespace Akka.Persistence.EventStore.Journal
{
    /// <summary>
    /// Extension point to "hijack" the deserialization of events. 
    /// If you have an existing EventStore DB that hasn't used Akka.Persistence, you most likely will need to use a custom deserialization method.
    /// </summary>
    public interface IDeserializer
    {
        IPersistentRepresentation GetRepresentation(Serializer serializer, RecordedEvent @event);
    }

    public class DefaultDeserializer : IDeserializer
    {
        public virtual IPersistentRepresentation GetRepresentation(Serializer serializer, RecordedEvent @event)
        {
            return (IPersistentRepresentation)serializer.FromBinary(@event.Data, typeof(IPersistentRepresentation));
        }
    }
}