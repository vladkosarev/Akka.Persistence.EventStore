using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence.Journal;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Akka.Serialization;

namespace Akka.Persistence.EventStore.Journal
{
    public class EventStoreJournal : AsyncWriteJournal
    {
        private const int _batchSize = 500;
        private readonly IEventStoreConnection _connection;
        private readonly Serializer _serializer;
        private readonly ILoggingAdapter _log;
        private readonly IDeserializer _deserializer;

        public EventStoreJournal()
        {
            _log = Context.GetLogger();

            var serialization = Context.System.Serialization;
            _serializer = serialization.FindSerializerForType(typeof(IPersistentRepresentation));

            var extension = EventStorePersistence.Instance.Apply(Context.System);
            var journalSettings = extension.JournalSettings;
            _deserializer = journalSettings.Deserializer;

            _connection = extension.JournalSettings.Connection;
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            try
            {                
                var slice = await _connection.ReadStreamEventsBackwardAsync(persistenceId, StreamPosition.End, 1, false);

                long sequence = 0;

                if (slice.Events.Any())
                    sequence = slice.Events.First().OriginalEventNumber + 1;

                return sequence;
            }
            catch (Exception e)
            {
                _log.Error(e, e.Message);
                throw;
            }
        }

        public override async Task ReplayMessagesAsync(string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> replayCallback)
        {
            try
            {
                if (toSequenceNr < fromSequenceNr || max == 0) return;
                if (fromSequenceNr == toSequenceNr) max = 1;
                if (toSequenceNr > fromSequenceNr && max == toSequenceNr) max = toSequenceNr - fromSequenceNr + 1;

                long count = 0;
                int start = ((int)fromSequenceNr - 1);
                var localBatchSize = _batchSize;
                StreamEventsSlice slice;
                do
                {
                    if (max == long.MaxValue && toSequenceNr > fromSequenceNr)
                    {
                        max = toSequenceNr - fromSequenceNr + 1;
                    }
                    if (max < localBatchSize)
                    {
                        localBatchSize = (int)max;
                    }
                    slice = await _connection.ReadStreamEventsForwardAsync(persistenceId, start, localBatchSize, false);

                    foreach (var @event in slice.Events)
                    {
                        var representation = _deserializer.GetRepresentation(_serializer, @event.OriginalEvent);
                        replayCallback(representation);
                        count++;
                        if (count == max) return;
                    }

                    start = slice.NextEventNumber;

                } while (!slice.IsEndOfStream);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error replaying messages for: {0}", persistenceId);
                throw;
            }
        }

        protected override async Task WriteMessagesAsync(IEnumerable<IPersistentRepresentation> messages)
        {
            try
            {
                foreach (var grouping in messages.GroupBy(x => x.PersistenceId))
                {
                    var stream = grouping.Key;

                    var representations = grouping.OrderBy(x => x.SequenceNr).ToArray();
                    var expectedVersion = (int)representations.First().SequenceNr - 2;

                    var events = representations.Select(x =>
                    {
                        var eventId = GuidUtility.Create(GuidUtility.IsoOidNamespace, string.Concat(stream, x.SequenceNr));                        
                        var data = _serializer.ToBinary(x);
                        var meta = new byte[0];
                        var payload = x.Payload;
                        if (payload.GetType().GetProperty("Metadata") != null)
                        {
                            var propType = payload.GetType().GetProperty("Metadata").PropertyType;                            
                            meta = _serializer.ToBinary(payload.GetType().GetProperty("Metadata").GetValue(x.Payload));
                        }
                        return new EventData(eventId, x.Payload.GetType().FullName, true, data, meta);
                    });

                    await _connection.AppendToStreamAsync(stream, expectedVersion < 0 ? ExpectedVersion.NoStream : expectedVersion, events);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Error writing messages to store");
                throw;
            }
        }

        protected override Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr, bool isPermanent)
        {
            return Task.FromResult<object>(null);
        }

        class ActorRefConverter : JsonConverter
        {
            private readonly IActorContext _context;

            public ActorRefConverter(IActorContext context)
            {
                _context = context;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((IActorRef)value).Path.ToStringWithAddress());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var value = reader.Value.ToString();

                ActorSelection selection = _context.ActorSelection(value);
                return selection.Anchor;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(IActorRef).IsAssignableFrom(objectType);
            }
        }
    }
}
