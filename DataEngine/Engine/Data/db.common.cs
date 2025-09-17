using Npgsql;
using NpgsqlTypes;
using pgTypes;
using Utilities;

namespace DB.Common;

public static class DBCommon
{

    public static void AddLimits(ref string queryText, int max, int start)
    {
        if (max > 0) queryText += $" LIMIT {max.ToString()}";
        if (start >= 0) queryText += $" OFFSET {start.ToString()}";
    }

    public static void AddOrder(ref string queryText, string orderBy)
    {
        queryText += " ORDER BY " + orderBy;
    }

    public static void AddParams(ref NpgsqlCommand cmd, IEnumerable<object> parameters, string[]? types = null)
    {
        if (types == null)
        {
            foreach (object param in parameters)
            {
                cmd.Parameters.AddWithValue(param);
            }
        }
        else
        {
            int index = 0;
            NpgsqlDbType typeArg;
            foreach (object param in parameters)
            {
                typeArg = pgType.Lookup(types[index++]);
                cmd.Parameters.AddWithValue(typeArg, param);
            }
        }
    }

    public static void AddParams(ref NpgsqlBatchCommand cmd, IEnumerable<object> parameters, string[]? types = null)
    {
        if (types == null)
        {
            foreach (object param in parameters)
            {
                cmd.Parameters.AddWithValue(param);
            }
        }
        else
        {
            int index = 0;
            NpgsqlDbType typeArg;
            foreach (object param in parameters)
            {
                typeArg = pgType.Lookup(types[index++]);
                cmd.Parameters.AddWithValue(typeArg, param);
            }
        }
    }

    public static void BuildQuerySection(IEnumerable<string> pathList, IEnumerable<object> valueList, IEnumerable<string>? operatorList, ref string queryText, ref Queue<object> parameters, ref List<string> paramTypes, string delim)
    {
        IEnumerator<string> pathEnumerator = pathList.GetEnumerator();
        IEnumerator<string>? opEnumerator = null;
        if (operatorList is not null) opEnumerator = operatorList.GetEnumerator();
        int paramCount = parameters.Count;
        bool firstLoop = true;
        string op = "=";

        foreach (object val in valueList)
        {
            if (!pathEnumerator.MoveNext() || (opEnumerator is not null && !opEnumerator.MoveNext())) CollectionCountException();
            if (!firstLoop)
            {
                queryText += delim;
            }
            if (opEnumerator is not null) op = opEnumerator.Current;
            object addVal = val;
            if (val.GetType().Name == "UInt128")
            {
                addVal = Utils.GetU128Bytes((UInt128)val);
            }

            queryText += $" {pathEnumerator.Current} {op} $" + (++paramCount).ToString();
            parameters.Enqueue(addVal);
            if (op.Contains('@'))
            {
                paramTypes.Add("jsonb");
            }
            else
            {
                paramTypes.Add(addVal.GetType().Name.ToLower());
            }
            firstLoop = false;
        }
        if (pathEnumerator.MoveNext() || (opEnumerator is not null && opEnumerator.MoveNext())) CollectionCountException();
    }


    public static void BuildQuerySection(IEnumerable<string> columns, IEnumerable<object> values, ref string queryText, ref Queue<object> parameters, string delim)
    {
        List<string> colList = [.. columns];
        int paramCount = parameters.Count;
        int index = 0;
        int maxIndex = colList.Count;

        foreach (object val in values)
        {
            if (index > maxIndex) CollectionCountException();
            if (index != 0) queryText += delim;
            queryText += $" {colList[index++]} = $" + (++paramCount).ToString();
            parameters.Enqueue(val);
        }
        if (maxIndex > index) CollectionCountException();
    }

    public static void CollectionCountException()
    {
        throw new ArgumentException("Collection arguments must have the same number of items.");
    }
}