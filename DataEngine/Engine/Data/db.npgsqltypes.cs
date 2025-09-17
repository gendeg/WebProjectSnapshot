using NpgsqlTypes;

namespace pgTypes;

public static class pgType
{
    public static NpgsqlDbType Lookup(string dataTypeName)
    {
        var unqualifiedName = dataTypeName;
        if (dataTypeName.IndexOf(".", StringComparison.Ordinal) is not -1 and var index)
            unqualifiedName = dataTypeName.Substring(0, index);

        return unqualifiedName switch
        {
            "jsonb" => NpgsqlDbType.Jsonb,
            "string" => NpgsqlDbType.Text,
            "bytea" => NpgsqlDbType.Bytea,
            "byte[]" => NpgsqlDbType.Bytea,
            "int" => NpgsqlDbType.Integer,

            // Numeric types
            "int2" => NpgsqlDbType.Smallint,
            "int4" => NpgsqlDbType.Integer,
            "int8" => NpgsqlDbType.Bigint,
            "float4" => NpgsqlDbType.Real,
            "float8" => NpgsqlDbType.Double,
            "numeric" => NpgsqlDbType.Numeric,
            "money" => NpgsqlDbType.Money,

            // Text types
            "text" => NpgsqlDbType.Text,
            "xml" => NpgsqlDbType.Xml,
            "varchar" => NpgsqlDbType.Varchar,
            "bpchar" => NpgsqlDbType.Char,
            "name" => NpgsqlDbType.Name,
            "refcursor" => NpgsqlDbType.Refcursor,
            "json" => NpgsqlDbType.Json,
            "jsonpath" => NpgsqlDbType.JsonPath,

            // Date/time types
            "timestamp" => NpgsqlDbType.Timestamp,
            "timestamptz" => NpgsqlDbType.TimestampTz,
            "date" => NpgsqlDbType.Date,
            "time" => NpgsqlDbType.Time,
            "timetz" => NpgsqlDbType.TimeTz,
            "interval" => NpgsqlDbType.Interval,

            // Network types
            "cidr" => NpgsqlDbType.Cidr,
            "inet" => NpgsqlDbType.Inet,
            "macaddr" => NpgsqlDbType.MacAddr,
            "macaddr8" => NpgsqlDbType.MacAddr8,

            // Full-text search types
            "tsquery" => NpgsqlDbType.TsQuery,
            "tsvector" => NpgsqlDbType.TsVector,

            // Geometry types
            "box" => NpgsqlDbType.Box,
            "circle" => NpgsqlDbType.Circle,
            "line" => NpgsqlDbType.Line,
            "lseg" => NpgsqlDbType.LSeg,
            "path" => NpgsqlDbType.Path,
            "point" => NpgsqlDbType.Point,
            "polygon" => NpgsqlDbType.Polygon,

            // UInt types
            "oid" => NpgsqlDbType.Oid,
            "xid" => NpgsqlDbType.Xid,
            "xid8" => NpgsqlDbType.Xid8,
            "cid" => NpgsqlDbType.Cid,
            "regtype" => NpgsqlDbType.Regtype,
            "regconfig" => NpgsqlDbType.Regconfig,

            // Misc types
            "bool" => NpgsqlDbType.Boolean,
            "uuid" => NpgsqlDbType.Uuid,
            "varbit" => NpgsqlDbType.Varbit,
            "bit" => NpgsqlDbType.Bit,

            // Built-in range types
            "int4range" => NpgsqlDbType.IntegerRange,
            "int8range" => NpgsqlDbType.BigIntRange,
            "numrange" => NpgsqlDbType.NumericRange,
            "tsrange" => NpgsqlDbType.TimestampRange,
            "tstzrange" => NpgsqlDbType.TimestampTzRange,
            "daterange" => NpgsqlDbType.DateRange,

            // Built-in multirange types
            "int4multirange" => NpgsqlDbType.IntegerMultirange,
            "int8multirange" => NpgsqlDbType.BigIntMultirange,
            "nummultirange" => NpgsqlDbType.NumericMultirange,
            "tsmultirange" => NpgsqlDbType.TimestampMultirange,
            "tstzmultirange" => NpgsqlDbType.TimestampTzMultirange,
            "datemultirange" => NpgsqlDbType.DateMultirange,

            // Internal types
            "int2vector" => NpgsqlDbType.Int2Vector,
            "oidvector" => NpgsqlDbType.Oidvector,
            "pg_lsn" => NpgsqlDbType.PgLsn,
            "tid" => NpgsqlDbType.Tid,
            "char" => NpgsqlDbType.InternalChar,

            // Plugin types
            "citext" => NpgsqlDbType.Citext,
            "lquery" => NpgsqlDbType.LQuery,
            "ltree" => NpgsqlDbType.LTree,
            "ltxtquery" => NpgsqlDbType.LTxtQuery,
            "hstore" => NpgsqlDbType.Hstore,
            "geometry" => NpgsqlDbType.Geometry,
            "geography" => NpgsqlDbType.Geography,

            _ when unqualifiedName.Contains("unknown")
                => !unqualifiedName.StartsWith("_", StringComparison.Ordinal)
                    ? NpgsqlDbType.Unknown
                    : throw new NotImplementedException(),
            _ when unqualifiedName.StartsWith("_", StringComparison.Ordinal)
                => Lookup(unqualifiedName.Substring(1)) is { } elementNpgsqlDbType
                    ? elementNpgsqlDbType | NpgsqlDbType.Array
                    : throw new NotImplementedException(),
            // e.g. custom ranges, plugin types etc.
            _ => throw new NotImplementedException()

        };
    }
}