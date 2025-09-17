using System.Collections;
using Apps;

namespace IAppTests;

public class BlankAppTests
{
    BlankApp app = new();

    [Fact]
    public void App_CanSetLocalThenGet()
    {
        app.SetValLocal("testKey", "testValue");
        Assert.Equal("testValue", app.GetVal("testKey"));
    }

    [Fact]
    public void App_CanSet()
    {
        app.SetVal("testKey", "testValue");
        Assert.Equal("testValue", app.GetVal("testKey"));
    }

    [Fact]
    public void App_CanSetManyLocalsThenGet()
    {
        app.SetManyValsLocal("{\"one\": \"value_one\", \"two\": \"value_two\"}");
        Assert.Equal("value_one", app.GetVal("one"));
        Assert.Equal("value_two", app.GetVal("two"));
    }

    [Fact]
    public void App_CanSetEnumThenGet()
    {
        object[] innerEnum = ["valTwo", 3];
        Dictionary<string, dynamic> testEnum = new() { { "valOne", innerEnum } };
        app.SetDictionaryLocal("testEnum", testEnum);

        var output = app.GetDictionary("testEnum").GetEnumerator();
        output.MoveNext();
        Assert.Equal("valOne", output.Current.Key);
        object[] innerOutput = (object[])output.Current.Value;
        Assert.Equal("valTwo", innerOutput[0]);
        Assert.Equal(3, innerOutput[1]);
    }

    [Fact]
    public void App_ValidateData()
    {
        app.SetVal("key1", "testValue");
        app.SetVal("key2", "testValue2");
        app.SetVal("key3", "testValue3");
        app.ValidateData("test");
        Assert.Equal("testValue", app.GetVal("key1"));
        Assert.Equal("testValue2", app.GetVal("key2"));
        Assert.Equal("testValue3", app.GetVal("key3"));
        Assert.Equal("{}", app.GetVal("key4"));
    }

    [Fact]
    public void App_ValidateDataThrowsErrorOnMissingRequired()
    {
        app.SetVal("key1", "testValue");
        Assert.Throws<ArgumentException>(() => app.ValidateData("test"));
    }
}


public class DataRegisterTests
{
    [Fact]
    public void CanGetRegisterDataAndIsCorrectType()
    {
        var data = DataRegister.Get("test");
        Assert.IsType<IDictionary[]>(data);
        Assert.IsType<Dictionary<string, string>>(data[0]);
        Assert.IsType<Dictionary<string, string[]>>(data[1]);
    }

    [Fact]
    public void RequiredTestDataMatches()
    {
        var data = DataRegister.Get("test")[0];
        Assert.Equal("text", data["key1"]);
        Assert.Equal("parts", data["key2"]);
    }

    [Fact]
    public void OptionalTestDataMatches()
    {
        var data = DataRegister.Get("test")[1];
        Assert.Equal("text", ((string[])data["key3"]!)[0]);
        Assert.Equal("defaults", ((string[])data["key3"]!)[1]);
        Assert.Equal("parts", ((string[])data["key4"]!)[0]);
        Assert.Equal("{}", ((string[])data["key4"]!)[1]);
    }

}