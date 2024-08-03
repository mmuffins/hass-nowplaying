using Tmds.DBus;

namespace NowPlayingDaemon;

public class DBusConnectionManager : IDisposable
{
    private readonly Connection connection;
    private bool isDisposed;

    public DBusConnectionManager()
    {
        connection = new Connection(Address.Session);
        connection.ConnectAsync().Wait();
    }

    public Connection Connection => connection;

    public void Dispose()
    {
        if (!isDisposed)
        {
            connection.Dispose();
            isDisposed = true;
        }
    }
}
