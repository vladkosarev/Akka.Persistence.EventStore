using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Event;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Akka.Persistence.EventStore
{
    public interface IConnectionFactory
    {
        ConnectionSettings CreateConnectionSettings(AkkaLogger logger);
        Task<IEventStoreConnection> CreateAsync(ILoggingAdapter log, string host, int tcpPort);
    }

    public class DefaultConnectionFactory : IConnectionFactory
    {
        public virtual ConnectionSettings CreateConnectionSettings(AkkaLogger logger)
        {
            return ConnectionSettings.Create()
                .KeepReconnecting()
                .UseCustomLogger(logger)
                .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
        }

        public async Task<IEventStoreConnection> CreateAsync(ILoggingAdapter log, string host, int tcpPort)
        {
            var address = (await Dns.GetHostAddressesAsync(host)).First();
            var connection = EventStoreConnection.Create(CreateConnectionSettings(new AkkaLogger(log)), new IPEndPoint(address, tcpPort));
            await connection.ConnectAsync();
            return connection;
        }
    }
}