using Akka.Configuration;
using Akka.Persistence.TestKit.Journal;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using System.Net;

namespace Akka.Persistence.EventStore.Tests
{
    public class EventStoreJournalSpec : JournalSpec
    {
        private static readonly Config SpecConfig = ConfigurationFactory.ParseString(@"
        akka.persistence {
            publish-plugin-commands = on

            journal {
                plugin = ""akka.persistence.journal.eventstore""
                eventstore {
                    class = ""Akka.Persistence.EventStore.Journal.EventStoreJournal, Akka.Persistence.EventStore""
                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                    connection-string = ""ConnectTo=tcp://admin:changeit@127.0.0.1:4567;""                    
                }
            }
        }");

        private static ClusterVNode Node;

        public EventStoreJournalSpec()
            : base(SpecConfig, "EventStoreJournalSpec")
        {
            Node = EmbeddedVNodeBuilder
                .AsSingleNode()
                .RunInMemory()
                .WithInternalTcpOn(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4566))
                .WithExternalTcpOn(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4567))
                .WithInternalHttpOn(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5566))
                .WithExternalHttpOn(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5567))
                .Build();
            Node.Start();
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Node.Stop();
        }
    }

}
