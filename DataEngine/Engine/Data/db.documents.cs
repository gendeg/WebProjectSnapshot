using Npgsql;
using DB.PostgreSQL;
using System.Text.Json;
using DB.Common;
using System.Text.Json.Nodes;
using Utilities;

namespace DB.Documents;


public class Documents : PgDocuments
{

    public JsonDocument? GetDoc(string type, UInt128 ID)
    {
        string queryText = $"SELECT doc FROM {type} WHERE id = $1";
        object[] parameters = [Utils.GetU128Bytes(ID)];

        NpgsqlDataReader pgResults = Read(queryText, parameters, ["bytea"]);
        if (!pgResults.HasRows) return null;
        pgResults.Read();
        return pgResults.GetFieldValue<JsonDocument>(0);
    }

    public object[]? GetDocByName(string type, string name)
    {
        string queryText = $"SELECT id, doc FROM {type} WHERE name = $1";
        object[] parameters = [name];

        NpgsqlDataReader pgResults = Read(queryText, parameters, ["string"]);
        if (!pgResults.HasRows) return null;
        pgResults.Read();
        byte[] rawId = pgResults.GetFieldValue<byte[]>(0);
        return [Utils.GetU128Val(rawId), pgResults.GetFieldValue<JsonDocument>(1)];
    }

    public List<JsonDocument>? GetManyDocs(string type, IEnumerable<string> pathList, IEnumerable<object> valueList, IEnumerable<string>? operatorList = null, int max = 1000, int start = -1, string orderBy = "")
    {
        string queryText = $"SELECT doc FROM {type} WHERE";
        Queue<object> parameters = new();
        List<string> paramTypes = [];

        DBCommon.BuildQuerySection(pathList, valueList, operatorList, ref queryText, ref parameters, ref paramTypes, delimiter);
        if (orderBy != "") DBCommon.AddOrder(ref queryText, orderBy);
        DBCommon.AddLimits(ref queryText, max, start);

        NpgsqlDataReader pgResults = Read(queryText, parameters, [.. paramTypes]);
        if (!pgResults.HasRows) return null;
        List<JsonDocument> results = [];
        while (pgResults.Read())
        {
            results.Add(pgResults.GetFieldValue<JsonDocument>(0));
        }
        return results;
    }

    public List<JsonDocument>? GetManyDocs(string type, string path, IEnumerable<UInt128> valueList, string? operatorArg = null, int max = 1000, int start = -1, string orderBy = "")
    {
        List<object> newValList = [.. valueList];
        return GetManyDocs(type, path, newValList, operatorArg, max, start, orderBy);
    }

    public List<JsonDocument>? GetManyDocs(string type, string path, object value, string? operatorArg = null, int max = 1000, int start = -1, string orderBy = "")
    {
        string[]? newOpArg = operatorArg is not null ? [operatorArg] : null;
        return GetManyDocs(type, [path], [value], newOpArg, max, start, orderBy);
    }

    public List<JsonDocument>? GetManyDocs(string type, string path, IEnumerable<object> valueList, string? operatorArg = null, int max = 1000, int start = -1, string orderBy = "")
    {
        string[]? operatorList;
        if (operatorArg == null)
        {
            operatorList = null;
        }
        else
        {
            operatorList = new string[valueList.Count()];
            for (int i = 0; i < operatorList.Length; i++) operatorList[i] = operatorArg;
        }

        string[] pathList = new string[valueList.Count()];
        for (int i = 0; i < pathList.Length; i++) pathList[i] = path;

        return GetManyDocs(type, pathList, valueList, operatorList, max, start, orderBy);
    }

    public UInt128? GetIDByName(string type, string name)
    {
        string queryText = $"SELECT id FROM {type} WHERE name = $1";
        object[] parameters = [name];

        NpgsqlDataReader pgResults = Read(queryText, parameters, ["string"]);
        if (!pgResults.HasRows) return null;
        pgResults.Read();
        byte[] rawId = pgResults.GetFieldValue<byte[]>(0);
        return Utils.GetU128Val(rawId);
    }

    public List<UInt128>? GetManyIDs(string type, IEnumerable<string> pathList, IEnumerable<object> valueList, IEnumerable<string>? operatorList = null, int max = 1000, int start = -1)
    {
        string queryText = $"SELECT id FROM {type} WHERE";
        Queue<object> parameters = new();
        List<string> paramTypes = [];

        DBCommon.BuildQuerySection(pathList, valueList, operatorList, ref queryText, ref parameters, ref paramTypes, delimiter);
        DBCommon.AddLimits(ref queryText, max, start);

        NpgsqlDataReader pgResults = Read(queryText, parameters, [.. paramTypes]);
        if (!pgResults.HasRows) return null;
        List<UInt128> results = [];
        while (pgResults.Read())
        {
            byte[] rawId = pgResults.GetFieldValue<byte[]>(0);
            results.Add(Utils.GetU128Val(rawId));
        }
        return results;
    }

    public List<UInt128>? GetManyIDs(string type, string path, object value, string? operatorArg = null, int max = 1000, int start = -1)
    {
        string[]? newOpArg = operatorArg is not null ? [operatorArg] : null;
        return GetManyIDs(type, [path], [value], newOpArg, max, start);
    }

    public List<UInt128>? GetManyIDs(string type, string path, IEnumerable<object> valueList, string? operatorArg = null, int max = 1000, int start = -1)
    {
        string[]? operatorList;
        if (operatorArg == null)
        {
            operatorList = null;
        }
        else
        {
            operatorList = new string[valueList.Count()];
            for (int i = 0; i < operatorList.Length; i++) operatorList[i] = operatorArg;
        }

        string[] pathList = new string[valueList.Count()];
        for (int i = 0; i < pathList.Length; i++) pathList[i] = path;

        return GetManyIDs(type, pathList, valueList, operatorList, max, start);
    }

    public object? GetValue(string type, UInt128 ID, string path)
    {
        NpgsqlDataReader pgResults = Read($"SELECT doc #> '{path}' AS value FROM {type} WHERE id = $1", [Utils.GetU128Bytes(ID)], ["byte[]"]);
        if (!pgResults.HasRows) return null;

        pgResults.Read();
        object value = pgResults.GetValue(0);
        if (value.GetType().Name == "DBNull") return null;
        return ParseJSONType((string)value);
    }

    public List<object?>? GetManyValues(string type, UInt128 ID, IEnumerable<string> pathList)
    {
        PgBatch batch = new(Sources.DocSource);
        foreach (string path in pathList)
        {
            batch.AddBatchCommand($"SELECT doc #> '{path}' AS value FROM {type} WHERE id = $1", [Utils.GetU128Bytes(ID)], ["byte[]"]);
        }

        NpgsqlDataReader pgResults = batch.ReadBatch();
        if (!pgResults.HasRows) return null;

        List<object?> results = [];
        do
        {
            pgResults.Read();
            object value = pgResults.GetValue(0);
            if (value.GetType().Name == "DBNull")
            {
                results.Add(null);
            }
            else
            {
                results.Add(ParseJSONType((string)value));
            }
        }
        while (pgResults.NextResult());

        return results;
    }

    private static object ParseJSONType(string input)
    {
        if (input[0] == '"' && input[^1] == '"') return input[1..^1];
        if (Int128.TryParse(input, out Int128 outInt)) return outInt;
        if (decimal.TryParse(input, out decimal outDec)) return outDec;
        if (input == "true") return true;
        if (input == "false") return false;
        return input;
    }

    public int Insert(string type, UInt128 ID, string name, char[] NewJSONDoc)
    {
        string queryText = $"INSERT INTO {type} (ID, name, doc) VALUES ($1, $2, $3)";
        Queue<object> parameters = new([Utils.GetU128Bytes(ID), name, NewJSONDoc]);
        string[] paramTypes = ["byte[]", "string", "jsonb"];

        return Execute(queryText, parameters, paramTypes);
    }

    public int Insert(string type, UInt128 ID, string name, string NewJSONDoc)
    {
        return Insert(type, ID, name, [.. NewJSONDoc]);
    }

    public int Insert(string type, UInt128 ID, string name, JsonDocument NewJSONDoc)
    {
        string JSONArr = JsonSerializer.Deserialize<string>(NewJSONDoc)!;
        return Insert(type, ID, name, JSONArr);
    }

    public int Insert(string type, UInt128 ID, string name, JsonNode NewJSONDoc)
    {
        char[] JSONArr = JsonSerializer.Deserialize<char[]>(NewJSONDoc)!;
        return Insert(type, ID, name, JSONArr);
    }

    public int InsertMany(string type, IEnumerable<UInt128> IDList, IEnumerable<string> nameList, IEnumerable<string> NewJSONDocList)
    {
        IEnumerator<UInt128> IDEnumerator = IDList.GetEnumerator();
        IEnumerator<string> nameEnumerator = nameList.GetEnumerator();
        PgBatch batch = new(Sources.DocSource);

        foreach (string JSON in NewJSONDocList)
        {
            if (!IDEnumerator.MoveNext() || !nameEnumerator.MoveNext()) DBCommon.CollectionCountException();
            object[] parameters = [Utils.GetU128Bytes(IDEnumerator.Current), nameEnumerator.Current, JSON];
            batch.AddBatchCommand($"INSERT INTO {type} (ID, name, doc) VALUES ($1, $2, $3)", parameters, ["byte[]", "string", "jsonb"]);
        }
        if (IDEnumerator.MoveNext() || nameEnumerator.MoveNext()) DBCommon.CollectionCountException();

        return batch.ExecuteBatchNoRead();
    }

    public int Update(string type, UInt128 ID, string path, string JSONString)
    {
        string queryText = $"UPDATE {type} SET doc = jsonb_set(doc, '{path}', $1) WHERE id = $2";
        Queue<object> parameters = new([JSONString, Utils.GetU128Bytes(ID)]);
        string[] paramTypes = ["jsonb", "byte[]"];

        return Execute(queryText, parameters, paramTypes);
    }

    public int Update(string type, UInt128 ID, string NewJSONDoc)
    {
        string queryText = $"UPDATE {type} SET doc = $1 WHERE id = $2";
        Queue<object> parameters = new([NewJSONDoc, Utils.GetU128Bytes(ID)]);
        string[] paramTypes = ["jsonb", "byte[]"];

        return Execute(queryText, parameters, paramTypes);
    }

    public int Update(string type, UInt128 ID, IEnumerable<string> pathList, IEnumerable<string> JSONStringList)
    {
        IEnumerator<string> stringEnumerator = JSONStringList.GetEnumerator();
        PgBatch batch = new(Sources.DocSource);

        foreach (string path in pathList)
        {
            if (!stringEnumerator.MoveNext()) DBCommon.CollectionCountException();
            object[] parameters = [stringEnumerator.Current, Utils.GetU128Bytes(ID)];
            batch.AddBatchCommand($"UPDATE {type} SET doc = jsonb_set(doc, '{path}', $1) WHERE id = $2", parameters, ["jsonb", "byte[]"]);
        }
        if (stringEnumerator.MoveNext()) DBCommon.CollectionCountException();

        return batch.ExecuteBatchNoRead();
    }

    public int UpdateMany(string type, IEnumerable<UInt128> IDList, IEnumerable<string> pathList, IEnumerable<string> JSONStringList)
    {
        IEnumerator<string> stringEnumerator = JSONStringList.GetEnumerator();
        PgBatch batch = new(Sources.DocSource);

        foreach (UInt128 ID in IDList)
        {
            stringEnumerator.Reset();
            foreach (string path in pathList)
            {
                if (!stringEnumerator.MoveNext()) DBCommon.CollectionCountException();
                object[] parameters = [stringEnumerator.Current, Utils.GetU128Bytes(ID)];
                batch.AddBatchCommand($"UPDATE {type} SET doc = jsonb_set(doc, '{path}', $1) WHERE id = $2", parameters, ["jsonb", "byte[]"]);
            }
            if (stringEnumerator.MoveNext()) DBCommon.CollectionCountException();
        }

        return batch.ExecuteBatchNoRead();
    }

    public int Delete(string type, UInt128 ID)
    {
        string queryText = $"DELETE FROM {type} WHERE id = $1";
        return Execute(queryText, [Utils.GetU128Bytes(ID)], ["byte[]"]);
    }

    public int DeleteMany(string type, IEnumerable<UInt128> IDList)
    {
        PgBatch batch = new(Sources.DocSource);
        foreach (UInt128 ID in IDList)
        {
            batch.AddBatchCommand($"DELETE FROM {type} WHERE id = $1", [Utils.GetU128Bytes(ID)], ["byte[]"]);
        }
        return batch.ExecuteBatchNoRead();
    }

    public int DeleteMany(string type, string JSONString, string operatorArg = "@>")
    {
        string queryText = $"DELETE FROM {type} WHERE doc {operatorArg} $1";
        return Execute(queryText, [JSONString], ["jsonb"]);
    }
}