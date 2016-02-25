using Akka.Configuration;
using Akka.Persistence.TestKit.Snapshot;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using System.Net;

namespace Akka.Persistence.EventStore.Tests
{
    public class EventStoreSnapshotSpec : SnapshotStoreSpec
    {
        private static readonly Config SpecConfig = ConfigurationFactory.ParseString(@"
        akka.persistence {
            publish-plugin-commands = on

            snapshot-store {
                plugin = ""akka.persistence.snapshot-store.eventstore""
                eventstore {
                    class = ""Akka.Persistence.Eventstore.Snapshot.EventStoreSnapshotStore, Akka.Persistence.EventStore""
                    plugin-dispatcher = ""akka.actor.default-dispatcher""
                    connection-string = ""ConnectTo=tcp://admin:changeit@127.0.0.1:4567;""                    
                }
            }
        }");

        private static ClusterVNode Node;

        public EventStoreSnapshotSpec()
            : base(SpecConfig, "EventStoreSnapshotSpec")
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
