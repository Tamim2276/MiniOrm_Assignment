using Npgsql;

public class DbContext : IDisposable
{
    protected readonly NpgsqlConnection _connection;

    public DbContext (string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }

    public NpgsqlConnection Connection => _connection;

    public void Dispose()
    {
        if(_connection != null && _connection.State != System.Data.ConnectionState.Closed)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}