using DB.RDBMS;

namespace RDBMSTests;

public class RDBMSTests
{

    [Fact]
    public void CheckRDBMSInterface()
    {
        RDBMS db = new RDBMS();
        string table = "test";
        db.Execute("DELETE FROM test WHERE key > 9", []);

        List<string> col = ["key", "value"];
        List<object> val = [500, "the 500!"];
        if (db.Insert(table, col, val) == 1)
        {
            val = [501, "EVEN MOAR!!!"];
            db.Insert(table, col, val);
        }

        string selectResult = "";
        if (db.Select("SELECT * FROM test WHERE key > $1", [499]))
        {
            while (db.NextRow())
            {
                selectResult += db.results!["key"] + ": " + db.results["value"] + " ";
            }
        }
        Assert.Equal("500: the 500! 501: EVEN MOAR!!! ", selectResult);

        col = ["value"];
        val = ["EVEN MORE!!!"];
        List<string> col2 = ["key"];
        List<object> val2 = [501];
        selectResult = "";
        if (db.Update(table, col, val, col2, val2) == 1)
        {
            db.Select("SELECT * FROM test WHERE key = 501", []);
            while (db.NextRow())
            {
                selectResult += db.results!["key"] + ": " + db.results["value"] + " ";
            }
        }
        Assert.Equal("501: EVEN MORE!!! ", selectResult);

        col = ["key"];
        val = [500];
        db.Delete(table, col, val);
        val = [501];
        if (db.Delete(table, col, val) == 1)
        {
            db.Select("SELECT * FROM test WHERE key > $1", [499]);
            selectResult = db.NextRow().ToString();
        }
        Assert.Equal("False", selectResult);
    }
}