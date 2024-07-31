using Tmds.DBus;

namespace MPRISInterface;

public class DBusConnectionManager : IDisposable
{
    private readonly Connection connection;
    private bool isDisposed;

    public DBusConnectionManager()
    {
        using var dd = new Connection(Address.Session);
        connection = new Connection(Address.Session);
        connection.ConnectAsync().Wait(); // Connect synchronously on startup
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