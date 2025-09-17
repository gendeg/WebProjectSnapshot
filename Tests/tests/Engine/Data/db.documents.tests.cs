using System.Text.Json;
using DB.Documents;
using Utilities;

namespace DocumentsTests;

public class DocumentTests
{

    Documents db = new();

    public DocumentTests()
    {
        db.Execute("DELETE FROM test WHERE 1 = 1", []);
        db.Execute("""INSERT INTO test (id, name, doc) VALUES ($1,'name1','{"docID": "1a", "field1": "data", "field2": [1, "2", "three", "IV", [5, "five", "V"]]}')""", [Utils.GetU128Bytes((UInt128)1)]);
        db.Execute("""INSERT INTO test (id, name, doc) VALUES ($1,'name2','{"docID": "1a", "field1": "lotsa data", "field2": [1, "2", "three", "IV", [5, "fiver", "V"]]}')""", [Utils.GetU128Bytes((UInt128)2)]);
    }

    [Fact]
    public void CheckGetDocs()
    {
        JsonDocument? doc = db.GetDoc("test", 1);
        Assert.NotNull(doc);
        Assert.Equal("data", doc.RootElement.GetProperty("field1").GetString());

        Assert.Null(db.GetDoc("test", 9));
    }

    [Fact]
    public void CheckGetDocsByName()
    {
        object[]? doc2 = db.GetDocByName("test", "name2");
        Assert.NotNull(doc2);
        UInt128 theID = (UInt128)doc2[0];
        JsonDocument theDoc = (JsonDocument)doc2[1];
        Assert.Equal((UInt128)2, theID);
        Assert.Equal("lotsa data", theDoc.RootElement.GetProperty("field1").GetString());

        Assert.Null(db.GetDocByName("test", "name25"));
    }

    [Fact]
    public void CheckGetManyDocs()
    {
        List<JsonDocument>? docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>");
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        Assert.Null(db.GetManyDocs("test", "doc", """{"docID": "does not exist"}""", "@>"));

        docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>", 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        JsonDocument theDoc = docs[0];
        Assert.Equal("data", theDoc.RootElement.GetProperty("field1").GetString());

        docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>", 1, 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        theDoc = docs[0];
        Assert.Equal("lotsa data", theDoc.RootElement.GetProperty("field1").GetString());

        db.SetOR();
        docs = db.GetManyDocs("test", "doc", ["""{"field2": [["five"]]}""", """{"field2": [["fiver"]]}"""], "@>");
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        docs = db.GetManyDocs("test", ["id", "doc"], [(UInt128)1, """{"field2": [["fiver"]]}"""], ["=", "@>"]);
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        docs = db.GetManyDocs("test", "id", [(UInt128)1, (UInt128)2]);
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        db.SetAND();
        docs = db.GetManyDocs("test", ["id", "doc"], [(UInt128)1, """{"docID": "1a"}"""], ["=", "@>"]);
        Assert.NotNull(docs);
        Assert.Single(docs);
    }

    [Fact]
    public void CheckGetIDByName()
    {
        UInt128? ID = db.GetIDByName("test", "name1");
        Assert.Equal((UInt128)1, ID);

        Assert.Null(db.GetIDByName("test", "name25"));
    }

    [Fact]
    public void CheckGetManyIDs()
    {
        List<UInt128>? docs = db.GetManyIDs("test", "doc", """{"docID": "1a"}""", "@>");
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        Assert.Null(db.GetManyIDs("test", "doc", """{"docID": "does not exist"}""", "@>"));

        docs = db.GetManyIDs("test", "doc", """{"docID": "1a"}""", "@>", 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        Assert.Equal((UInt128)1, docs[0]);

        docs = db.GetManyIDs("test", "doc", """{"docID": "1a"}""", "@>", 1, 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        Assert.Equal((UInt128)2, docs[0]);

        db.SetOR();
        docs = db.GetManyIDs("test", "doc", ["""{"field2": [["five"]]}""", """{"field2": [["fiver"]]}"""], "@>");
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        docs = db.GetManyIDs("test", ["id", "doc"], [(UInt128)1, """{"field2": [["fiver"]]}"""], ["=", "@>"]);
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        db.SetAND();
        docs = db.GetManyIDs("test", ["id", "doc"], [(UInt128)1, """{"docID": "1a"}"""], ["=", "@>"]);
        Assert.NotNull(docs);
        Assert.Single(docs);
    }

    [Fact]
    public void CheckGetValues()
    {
        var result = db.GetValue("test", (UInt128)1, "{field1}");
        Assert.NotNull(result);
        Assert.Equal("data", result);

        Assert.Null(db.GetValue("test", (UInt128)1, "{field25}"));

        List<object?>? results = db.GetManyValues("test", (UInt128)2, ["{field1}", "{field2,4,0}", "{field25}"]);
        Assert.NotNull(results);
        Assert.Equal(["lotsa data", (Int128)5, null], results);
    }

    [Fact]
    public void CheckInsert()
    {
        db.Execute("DELETE FROM test WHERE 1 = 1", []);

        db.Insert("test", (UInt128)1, "name1", """{"docID": "1a", "field1": "data", "field2": [1, "2", "three", "IV", [5, "five", "V"]]}""");

        string JSON = """{"docID": "1a", "field1": "lotsa data", "field2": [1, "2", "three", "IV", [5, "fiver", "V"]]}""";
        JsonDocument InsertJSON = JsonSerializer.SerializeToDocument(JSON, JSON.GetType());
        db.Insert("test", (UInt128)2, "name2", InsertJSON);

        List<JsonDocument>? docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>", 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        JsonDocument theDoc = docs[0];
        Assert.Equal("data", theDoc.RootElement.GetProperty("field1").GetString());

        docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>", 1, 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        theDoc = docs[0];
        Assert.Equal("lotsa data", theDoc.RootElement.GetProperty("field1").GetString());

        db.SetOR();
        docs = db.GetManyDocs("test", "doc", ["""{"field2": [["five"]]}""", """{"field2": [["fiver"]]}"""], "@>");
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);
    }

    [Fact]
    public void CheckInsertMany()
    {
        db.Execute("DELETE FROM test WHERE 1 = 1", []);

        List<string> JSONList = [];
        JSONList.Add("""{"docID": "1a", "field1": "data", "field2": [1, "2", "three", "IV", [5, "five", "V"]]}""");
        JSONList.Add("""{"docID": "1a", "field1": "lotsa data", "field2": [1, "2", "three", "IV", [5, "fiver", "V"]]}""");

        db.InsertMany("test", [(UInt128)1, (UInt128)2], ["name1", "name2"], JSONList);

        List<JsonDocument>? docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>", 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        JsonDocument theDoc = docs[0];
        Assert.Equal("data", theDoc.RootElement.GetProperty("field1").GetString());

        docs = db.GetManyDocs("test", "doc", """{"docID": "1a"}""", "@>", 1, 1);
        Assert.NotNull(docs);
        Assert.Single(docs);
        theDoc = docs[0];
        Assert.Equal("lotsa data", theDoc.RootElement.GetProperty("field1").GetString());

        db.SetOR();
        docs = db.GetManyDocs("test", "doc", ["""{"field2": [["five"]]}""", """{"field2": [["fiver"]]}"""], "@>");
        Assert.NotNull(docs);
        Assert.Equal(2, docs.Count);

        Assert.Throws<ArgumentException>(() => db.InsertMany("test", [(UInt128)1, (UInt128)2], ["name1", "name2"], ["1"]));
        Assert.Throws<ArgumentException>(() => db.InsertMany("test", [(UInt128)1, (UInt128)2], ["name1"], ["1", "2"]));
    }

    [Fact]
    public void CheckUpdate()
    {
        db.Update("test", (UInt128)2, "{docID}", "\"2a\"");
        var result = db.GetValue("test", (UInt128)2, "{docID}");
        Assert.Equal("2a", result);

        db.Update("test", (UInt128)2, ["{field2,4,0}", "{field3}"], ["55", "\"new field\""]);
        var result2 = db.GetManyValues("test", (UInt128)2, ["{field2,4,0}", "{field3}"]);
        Assert.Equal([(Int128)55, "new field"], result2);

        db.Update("test", (UInt128)2, """{"docID": "1a", "field1": "data", "field2": [1, "2", "three", "IV", [5, "five", "V"]]}""");
        var result3 = db.GetManyValues("test", (UInt128)2, ["{field2,4,0}", "{docID}"]);
        Assert.Equal([(Int128)5, "1a"], result3);
        Assert.Null(db.GetValue("test", (UInt128)2, "{field3}"));
    }

    [Fact]
    public void CheckUpdateMany()
    {
        db.UpdateMany("test", [(UInt128)1, (UInt128)2], ["{docID}", "{field1}"], ["\"2b\"", "\"unified data\""]);
        Assert.Equal(["2b", "unified data"], db.GetManyValues("test", (UInt128)1, ["{docID}", "{field1}"]));
        Assert.Equal(["2b", "unified data"], db.GetManyValues("test", (UInt128)2, ["{docID}", "{field1}"]));
    }

    [Fact]
    public void CheckDelete()
    {
        Assert.Equal("data", db.GetValue("test", (UInt128)1, "{field1}"));
        db.Delete("test", (UInt128)1);
        Assert.Null(db.GetValue("test", (UInt128)1, "{field1}"));
    }

    [Fact]
    public void CheckDeleteMany1()
    {
        Assert.Equal("data", db.GetValue("test", (UInt128)1, "{field1}"));
        db.DeleteMany("test", [(UInt128)1, (UInt128)2]);
        Assert.Null(db.GetValue("test", (UInt128)1, "{field1}"));
        Assert.Null(db.GetValue("test", (UInt128)2, "{field1}"));
    }

    [Fact]
    public void CheckDeleteMany2()
    {
        Assert.Equal("data", db.GetValue("test", (UInt128)1, "{field1}"));
        db.DeleteMany("test", """{"docID": "1a"}""");
        Assert.Null(db.GetValue("test", (UInt128)1, "{field1}"));
        Assert.Null(db.GetValue("test", (UInt128)2, "{field1}"));
    }
}