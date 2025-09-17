using Cassandra;
using DB.ScyllaDB;
using AppSettings;
using System.Numerics;
using System.Security.Cryptography;
using DB.Common;
using Utilities;

namespace DB.WideColumns;

public class WideColumn : Scylla
{
    private void CounterUpdate(byte[] bucket_id, byte[] random)
    {
        int last11Bits = GetLast11Bits(random);
        if (last11Bits == 2047)
        {
            IncrementBucketCounter(bucket_id);
            buckets.UpdateBucket(bucket_id);

            object[] data = buckets.GetBucketData(bucket_id);
            DateTime lastChange = (DateTime)data[2];
            string bucketState = GetBucketState(data[1], lastChange);
            int counter = (int)data[3];

            if (counter >= 12 && bucketState == "SteadyState")
            {
                // find the later of "6 hours from now" and "last change + N days" to use for the next bucket change
                DateTime nowPlus6 = DateTime.UtcNow.AddHours(6);
                DateTime lastPlusDays = lastChange.AddHours(24 * (int)Settings.GetInt("minimumScyllaBucketDays")!);
                DateTime nextChange = (nowPlus6 > lastPlusDays ? nowPlus6 : lastPlusDays);

                TriggerBucketChange(bucket_id, nextChange);
            }

            if (bucketState == "PostChange")
            {
                ResetBucketCounter(bucket_id);
            }
            buckets.UpdateBucket(bucket_id);
        }
    }

    private static string GetBucketState(object nextChange, DateTime lastChange)
    {
        string result = "";
        DateTime now = DateTime.UtcNow;

        if (lastChange <= now && nextChange is null) result = "SteadyState";
        else if (lastChange > now) result = "PreChange";
        else if (lastChange <= now && nextChange is not null) result = "PostChange";

        return result;
    }

    private int GetCurrentBucket(byte[] bucket_id)
    {
        object[] data = buckets.GetBucketData(bucket_id);
        DateTime lastChange = (DateTime)data[2];
        string bucketState = GetBucketState(data[1], lastChange);

        if (bucketState == "PreChange")
        {
            return (int)data[0] - 1;
        }
        return (int)data[0];
    }

    private static int GetLast11Bits(byte[] random)
    {
        byte lastByte = random[7];
        byte secondLastByte = random[6];
        int secondLastBits = (secondLastByte & 0b00000111);
        return (secondLastBits << 8) | lastByte;
    }

    private Session? ParseRequestType(string type, ref string table, ref byte[] bucket_id, UInt128 group_id, UInt128? secondary_id = null)
    {
        object[] typeInfo = types[type];
        table = (string)typeInfo[1];
        if ((string)typeInfo[0] == "single")
        {
            bucket_id = Utils.GetU128Bytes(group_id);
            return (Session)((Cluster)typeInfo[2]).Connect((string)typeInfo[3]);
        }
        else if ((string)typeInfo[0] == "double" && secondary_id is not null)
        {
            bucket_id = new byte[32];
            Utils.GetU128Bytes(group_id).CopyTo(bucket_id, 0);
            Utils.GetU128Bytes((UInt128)secondary_id).CopyTo(bucket_id, 16);
            return (Session)((Cluster)typeInfo[2]).Connect((string)typeInfo[3]);
        }
        return null;
    }

    private static List<Dictionary<string, object>> RowSetToDictionaryList(RowSet results)
    {
        List<Dictionary<string, object>> list = [];

        foreach (Row row in results)
        {
            // Row = [item_id, secondary_id, content, properties]
            Dictionary<string, object> dict = new(4)
            {
                { "item_id", row[0] },
                { "secondary_id", row[1] },
                { "content", row[2] },
                { "properties", row[3] }
            };
            list.Add(dict);
        }
        return list;
    }

    public UInt128 Insert(string type, UInt128 group_id, UInt128 secondary_id, string content, string properties = "")
    {
        string table = "";
        byte[] bucket_id = [];
        Session session = ParseRequestType(type, ref table, ref bucket_id, group_id, secondary_id)!;

        byte[] random = RandomNumberGenerator.GetBytes(8);
        CounterUpdate(bucket_id, random);
        int bucket = GetCurrentBucket(bucket_id);
        BigInteger item_id = Utils.CreateItemID(random);

        string queryText = $"INSERT INTO {table} (group_id, secondary_id, bucket, item_id, content, properties) VALUES (?, ?, ?, ?, ?, ?)";
        object[] parameters = [(BigInteger)group_id, (BigInteger)secondary_id, bucket, item_id, content, properties];

        PreparedStatement statementPrep = session.Prepare(queryText);
        BoundStatement statement = statementPrep.Bind(parameters);

        // TODO catch failures and return 0
        session.Execute(statement);
        return (UInt128)item_id;
    }

    public List<Dictionary<string, object>> GetRows(string type, UInt128 group_id, int numRows = 50, UInt128? start_item = null, string operatorArg = ">")
    {
        return GetRows(type, group_id, null, numRows, start_item, operatorArg);
    }

    public List<Dictionary<string, object>> GetRows(string type, UInt128 group_id, UInt128? secondary_id, int numRows = 50, UInt128? start_item = null, string operatorArg = ">", int? bucket = null)
    {
        string table = "";
        byte[] bucket_id = [];
        Session session = ParseRequestType(type, ref table, ref bucket_id, group_id, secondary_id)!;
        bucket ??= GetCurrentBucket(bucket_id);

        string queryText = $"SELECT item_id, secondary_id, content, properties FROM {table} WHERE group_id = ?";
        Queue<object> parameters = new([(BigInteger)group_id]);

        if (secondary_id is not null)
        {
            queryText += $" AND secondary_id = ?";
            parameters.Enqueue((BigInteger)secondary_id);
        }
        queryText += $" AND bucket = ?";
        parameters.Enqueue(bucket);
        if (start_item is not null)
        {
            queryText += $" AND item_id {operatorArg} ?";
            parameters.Enqueue((BigInteger)start_item);
        }

        if (numRows > 0)
        {
            queryText += $" LIMIT ?";
            parameters.Enqueue(numRows);
        }

        PreparedStatement statementPrep = session.Prepare(queryText);
        BoundStatement statement = statementPrep.Bind([.. parameters]);
        RowSet results = session.Execute(statement);
        var returnList = RowSetToDictionaryList(results);

        int numResults = returnList.Count;
        if (numResults < numRows && bucket > 0)
        {
            var extraList = GetRows(type, group_id, secondary_id, (numRows - numResults), null, operatorArg, bucket - 1);
            returnList.AddRange(extraList);
        }

        return returnList;
    }

    public void Update(string type, UInt128 group_id, UInt128 item_id, IEnumerable<string> keyList, IEnumerable<object> valueList)
    {
        Update(type, group_id, null, item_id, keyList, valueList);
    }

    public void Update(string type, UInt128 group_id, UInt128? secondary_id, UInt128 item_id, IEnumerable<string> keyList, IEnumerable<object> valueList)
    {
        string table = "";
        byte[] bucket_id = [];
        Session session = ParseRequestType(type, ref table, ref bucket_id, group_id, secondary_id)!;
        int bucket = GetCurrentBucket(bucket_id);

        string queryText = $"UPDATE {table} SET";
        Queue<object> parameters = new();
        IEnumerator<string> keyEnumerator = keyList.GetEnumerator();

        foreach (object value in valueList)
        {
            if (!keyEnumerator.MoveNext()) DBCommon.CollectionCountException();
            queryText += $" {keyEnumerator.Current} = ?,";
            parameters.Enqueue(value);
        }
        queryText = queryText[0..^1];
        queryText += $" WHERE group_id = ?";
        parameters.Enqueue((BigInteger)group_id);
        if (secondary_id is not null)
        {
            queryText += $" AND secondary_id = ?";
            parameters.Enqueue((BigInteger)secondary_id);
        }
        queryText += $" AND bucket = ? AND item_id = ?";
        parameters.Enqueue(bucket);
        parameters.Enqueue((BigInteger)item_id);

        PreparedStatement statementPrep = session.Prepare(queryText);
        BoundStatement statement = statementPrep.Bind([.. parameters]);
        session.Execute(statement);
    }

    public void Delete(string type, UInt128 group_id, UInt128 item_id)
    {
        Delete(type, group_id, null, item_id);
    }

    public void Delete(string type, UInt128 group_id, UInt128? secondary_id, UInt128 item_id)
    {
        string table = "";
        byte[] bucket_id = [];
        Session session = ParseRequestType(type, ref table, ref bucket_id, group_id, secondary_id)!;
        int bucket = GetCurrentBucket(bucket_id);

        string queryText = $"DELETE FROM {table} WHERE group_id = ?";
        Queue<object> parameters = new([(BigInteger)group_id]);
        if (secondary_id is not null)
        {
            queryText += $" AND secondary_id = ?";
            parameters.Enqueue((BigInteger)secondary_id);
        }
        queryText += $" AND bucket = ? AND item_id = ?";
        parameters.Enqueue(bucket);
        parameters.Enqueue((BigInteger)item_id);

        PreparedStatement statementPrep = session.Prepare(queryText);
        BoundStatement statement = statementPrep.Bind([.. parameters]);
        session.Execute(statement);
    }

    public void DeleteMany(string type, UInt128 group_id, string key, object value, string operatorArg = "=")
    {
        DeleteMany(type, group_id, null, key, value, operatorArg);
    }

    public void DeleteMany(string type, UInt128 group_id, UInt128? secondary_id, string key, object value, string operatorArg = "=")
    {
        string table = "";
        byte[] bucket_id = [];
        Session session = ParseRequestType(type, ref table, ref bucket_id, group_id, secondary_id)!;
        int bucket = GetCurrentBucket(bucket_id);

        string queryText = $"DELETE FROM {table} WHERE group_id = ?";
        List<object> parameters = new([(BigInteger)group_id]);
        if (secondary_id is not null)
        {
            queryText += $" AND secondary_id = ?";
            parameters.Add((BigInteger)secondary_id);
        }
        queryText += $" AND bucket = ? AND {key} {operatorArg} ?";
        parameters.Add(bucket);
        if (value.GetType().Name == "UInt128") value = (BigInteger)(UInt128)value;
        parameters.Add(value);

        PreparedStatement statementPrep = session.Prepare(queryText);
        BoundStatement statement = statementPrep.Bind([.. parameters]);
        session.Execute(statement);
    }
}