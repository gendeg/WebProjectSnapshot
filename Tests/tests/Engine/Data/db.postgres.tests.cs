using DB.PostgreSQL;
using Npgsql;

namespace PostgresTests;

public class PostgresTests
{

    [Fact]
    public void CheckRDBMSInterfaceCRUD()
    {
        PgRDBMS db = new();
        db.Execute("DELETE FROM test WHERE key > 9", []);

        string cQuery = "INSERT INTO test (key, value) VALUES ($1, $2)";
        object[] cParamList = [10, "Test Value"];
        db.Execute(cQuery, cParamList);

        string rQuery = "SELECT * FROM test WHERE key = $1";
        object[] rParamList = [10];
        NpgsqlDataReader result = db.Read(rQuery, rParamList)!;
        result.Read();
        string compareVal = result["key"] + ": " + result["value"];
        Assert.Equal("10: Test Value", compareVal);

        string uQuery = "UPDATE test SET value = $1 WHERE key = $2";
        object[] uParamList = ["New Test Value", 10];
        db.Execute(uQuery, uParamList);

        result = db.Read(rQuery, rParamList)!;
        result.Read();
        compareVal = result["key"] + ": " + result["value"];
        Assert.Equal("10: New Test Value", compareVal);

        string dQuery = "DELETE FROM test WHERE key = $1";
        object[] dParamList = [10];
        db.Execute(dQuery, dParamList);

        result = db.Read(rQuery, rParamList)!;
        Assert.False(result.HasRows);
    }
}