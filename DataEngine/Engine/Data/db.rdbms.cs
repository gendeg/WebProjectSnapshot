using Npgsql;
using DB.PostgreSQL;
using DB.Common;

namespace DB.RDBMS;


public class RDBMS : PgRDBMS
{
    public NpgsqlDataReader? results;

    public bool NextRow()
    {
        if (results is null) return false;
        return results.Read();
    }

    public int Delete(string table, IEnumerable<string> whereColumns, IEnumerable<object> whereValues)
    {
        string queryText = $"DELETE FROM {table} WHERE";
        Queue<object> parameters = new();

        DBCommon.BuildQuerySection(whereColumns, whereValues, ref queryText, ref parameters, delimiter);

        return Execute(queryText, parameters);
    }

    public int Insert(string table, IEnumerable<string> columns, IEnumerable<object> values)
    {
        string queryText = $"INSERT INTO {table} (";

        Queue<object> parameters = new();
        int paramCount = 0;
        int countCheck = 0;

        foreach (string col in columns)
        {
            queryText += $"{col}, ";
            countCheck++;
        }

        queryText = queryText[..^2];
        queryText += ") VALUES (";

        foreach (object val in values)
        {
            queryText += "$" + (++paramCount).ToString() + ", ";
            parameters.Enqueue(val);
            countCheck--;
        }

        if (countCheck != 0) DBCommon.CollectionCountException();
        queryText = queryText[..^2];
        queryText += ")";

        return Execute(queryText, parameters);
    }

    public bool Select(string queryText, IEnumerable<object> parameters)
    {
        if (queryText.Substring(0, 6) != "SELECT") throw new ArgumentException("Must use a SELECT query");
        results?.Dispose();
        results = Read(queryText, parameters);
        return results.HasRows;
    }

    public int Update(string table, IEnumerable<string> setColumns, IEnumerable<object> setValues, IEnumerable<string> whereColumns, IEnumerable<object> whereValues)
    {
        string queryText = $"UPDATE {table} SET";
        Queue<object> parameters = new();

        DBCommon.BuildQuerySection(setColumns, setValues, ref queryText, ref parameters, ",");
        queryText += " WHERE";
        DBCommon.BuildQuerySection(whereColumns, whereValues, ref queryText, ref parameters, delimiter);

        return Execute(queryText, parameters);
    }

}