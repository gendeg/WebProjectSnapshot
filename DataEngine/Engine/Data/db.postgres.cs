using Npgsql;
using System.Data;
using AppSettings;
using DB.Common;

namespace DB.PostgreSQL;


public abstract class Postgres
{
    internal PgSources Sources = PgSources.Get();
    internal NpgsqlConnection? connection;
    internal string delimiter = " AND";

    public void Connect()
    {
        connection?.Open();
    }

    public void Close()
    {
        connection?.Close();
    }

    public int Execute(string queryText, IEnumerable<object> parameters)
    {
        NpgsqlCommand cmd = connection!.CreateCommand();
        cmd.CommandText = queryText;
        DBCommon.AddParams(ref cmd, parameters);
        connection.Close();
        connection.Open();
        int results = cmd.ExecuteNonQuery();
        return results;
    }

    public NpgsqlDataReader Read(string queryText, IEnumerable<object> parameters, CommandBehavior behavior = CommandBehavior.Default)
    {
        NpgsqlCommand cmd = connection!.CreateCommand();
        cmd.CommandText = queryText;
        DBCommon.AddParams(ref cmd, parameters);
        connection.Close();
        connection.Open();
        NpgsqlDataReader results = cmd.ExecuteReader(behavior);
        return results;
    }

    public void SetOR()
    {
        delimiter = " OR";
    }

    public void SetAND()
    {
        delimiter = " AND";
    }
}


public class PgBatch
{
    internal NpgsqlBatch batch;

    public PgBatch(NpgsqlDataSource dataSource)
    {
        batch = new(dataSource.CreateConnection());
        batch.Connection!.Open();
    }

    public void NewBatch(NpgsqlDataSource dataSource)
    {
        batch?.Dispose();
        batch = new(dataSource.CreateConnection());
        batch.Connection!.Open();
    }

    public void AddBatchCommand(string queryText, IEnumerable<object> parameters, string[]? types = null)
    {
        if (batch is null) NoBatchException();
        NpgsqlBatchCommand cmd = new(queryText);
        DBCommon.AddParams(ref cmd, parameters, types);
        batch!.BatchCommands.Add(cmd);
    }

    public int ExecuteBatchNoRead()
    {
        if (batch is null) NoBatchException();
        int rowsAffected = batch!.ExecuteNonQuery();
        batch.Dispose();
        return rowsAffected;
    }

    public NpgsqlDataReader ReadBatch(CommandBehavior behavior = CommandBehavior.Default)
    {
        if (batch is null) NoBatchException();
        NpgsqlDataReader results = batch!.ExecuteReader(behavior);
        batch.Dispose();
        return results;
    }

    public static void NoBatchException()
    {
        throw new ApplicationException("This data source does not have a batch innitiated; did you run NewBatch()?");
    }
}


public class PgRDBMS : Postgres
{
    public PgRDBMS()
    {
        connection = Sources.RDBMSSource.CreateConnection();
    }
}


public class PgDocuments : Postgres
{
    public PgDocuments()
    {
        connection = Sources.DocSource.CreateConnection();
    }

    public int Execute(string queryText, IEnumerable<object> parameters, string[] types)
    {
        NpgsqlCommand cmd = connection!.CreateCommand();
        cmd.CommandText = queryText;
        DBCommon.AddParams(ref cmd, parameters, types);
        connection.Close();
        connection.Open();
        int results = cmd.ExecuteNonQuery();
        return results;
    }

    public NpgsqlDataReader Read(string queryText, IEnumerable<object> parameters, string[] types, CommandBehavior behavior = CommandBehavior.Default)
    {
        NpgsqlCommand cmd = connection!.CreateCommand();
        cmd.CommandText = queryText;
        DBCommon.AddParams(ref cmd, parameters, types);
        connection.Close();
        connection.Open();
        NpgsqlDataReader results = cmd.ExecuteReader(behavior);
        return results;
    }
}


public class PgBuckets : Postgres
{
    public PgBuckets()
    {
        connection = Sources.CountSource.CreateConnection();
    }
}


public class PgSources
{
    private static PgSources? instance;
    private static readonly Lock _lock = new();
    public NpgsqlDataSource DocSource;
    public NpgsqlDataSource RDBMSSource;
    public NpgsqlDataSource CountSource;

    private PgSources()
    {
        string connectionString = Settings.GetString("pgCountersConnection")!;
        CountSource = NpgsqlDataSource.Create(connectionString);

        connectionString = Settings.GetString("pgDocsConnection")!;
        DocSource = NpgsqlDataSource.Create(connectionString);

        connectionString = Settings.GetString("pgRDBMSConnection")!;
        RDBMSSource = NpgsqlDataSource.Create(connectionString);
    }

    public static PgSources Get()
    {
        if (instance == null)
        {
            lock (_lock)
            {
                instance ??= new PgSources();
            }
        }
        return instance;
    }
}