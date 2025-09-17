using System.Numerics;
using DB.WideColumns;

namespace WideColumnsTests;

public class WideColumnTests
{
    WideColumn db = new();
    readonly UInt128 ID1 = 1;
    readonly UInt128 ID2 = 2;
    readonly UInt128 ID3 = 514;
    readonly UInt128 minId = new(65536, 0);

    public WideColumnTests()
    {
        db.DeleteMany("test_single", ID1, "item_id", 1, ">");
        db.DeleteMany("test_single", ID2, "item_id", 1, ">");
        db.DeleteMany("test_double", ID1, ID2, "item_id", 1, ">");
        db.DeleteMany("test_double", ID2, ID3, "item_id", 1, ">");
    }

    [Fact]
    public void CheckInsertAndRead()
    {
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID2, ID3, "Another Insert", """{"prop1": "single value"}""");
        db.Insert("test_double", ID1, ID2, "Test Insert (Double)", "Properties");
        db.Insert("test_double", ID2, ID3, "Another Insert", """{"prop1": "double value"}""");

        List<Dictionary<string, object>> result = db.GetRows("test_single", ID1);
        UInt128 id = (UInt128)(BigInteger)result[0]["item_id"];
        Assert.True(id > minId);
        UInt128 id2 = (UInt128)(BigInteger)result[0]["secondary_id"];
        Assert.Equivalent(ID2, id2, true);
        Assert.Equal("Test Insert (Single)", (string)result[0]["content"]);
        Assert.Equal("Properties", (string)result[0]["properties"]);

        result = db.GetRows("test_double", ID1, ID2);
        id = (UInt128)(BigInteger)result[0]["item_id"];
        Assert.True(id > minId);
        id2 = (UInt128)(BigInteger)result[0]["secondary_id"];
        Assert.Equivalent(ID2, id2, true);
        Assert.Equal("Test Insert (Double)", (string)result[0]["content"]);
        Assert.Equal("Properties", (string)result[0]["properties"]);
    }

    [Fact]
    public void CheckDelete()
    {
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        var result = db.GetRows("test_single", ID1);
        UInt128 id = (UInt128)(BigInteger)result[0]["item_id"];

        db.Delete("test_single", ID1, id);
        result = db.GetRows("test_single", ID1);
        Assert.Empty(result);

        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");

        result = db.GetRows("test_single", ID1);
        Assert.Equal(20, result.Count);
        db.DeleteMany("test_single", ID1, "item_id", minId, ">");
        result = db.GetRows("test_single", ID1);
        Assert.Empty(result);

    }

    [Fact]
    public void CheckUpdate()
    {
        db.Insert("test_single", ID1, ID2, "Test Insert (Single)", "Properties");
        var result = db.GetRows("test_single", ID1);
        UInt128 id = (UInt128)(BigInteger)result[0]["item_id"];

        db.Update("test_single", ID1, id, ["content", "properties"], ["new content", "{new properties}"]);
        result = db.GetRows("test_single", ID1);
        Assert.Equal("new content", (string)result[0]["content"]);
        Assert.Equal("{new properties}", (string)result[0]["properties"]);
    }

}