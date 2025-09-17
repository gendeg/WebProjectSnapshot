using Sessions;
using System.Text;

namespace SessionTests;

public class CurrentSessionTests
{

    [Fact]
    public void Test_UserToString_DefaultUser()
    {
        SessionUser user = new("test");
        user.Id = (UInt128)Math.Pow(2, 64) + 100;
        string result = CurrentSession.UserToString(user);
        string expectedResult = """
{"DisplayName":"","IdConfirmed":false,"Restrictions":0,"AccountType":0,"SubLevel":0,"Suspended":false,"SuspensionEnd":null,"Id":"18446744073709551716","UserName":"test","NormalizedUserName":"TEST","Email":null,"NormalizedEmail":null,"EmailConfirmed":false,"PasswordHash":null,"SecurityStamp":null,"ConcurrencyStamp":"00000000-0000-0000-0000-000000000000","PhoneNumber":null,"PhoneNumberConfirmed":false,"TwoFactorEnabled":false,"LockoutEnd":null,"LockoutEnabled":false,"AccessFailedCount":0}
""";
        // mask dynamic value that is different every instance
        string maskedResult = new StringBuilder(result[0..317])
            .Append("00000000-0000-0000-0000-000000000000")
            .Append(result[353..])
            .ToString();
        Assert.Equal(expectedResult, maskedResult);
    }

    [Fact]
    public void Test_StringToUser_DefaultUser()
    {

        string inputString = """
{"DisplayName":"Test Display","IdConfirmed":false,"Restrictions":0,"AccountType":0,"SubLevel":0,"Suspended":false,"SuspensionEnd":null,"Id":18446744073709551716,"UserName":"test","NormalizedUserName":"TEST","Email":null,"NormalizedEmail":null,"EmailConfirmed":false,"PasswordHash":null,"SecurityStamp":null,"ConcurrencyStamp":"00000000-0000-0000-0000-000000000000","PhoneNumber":null,"PhoneNumberConfirmed":false,"TwoFactorEnabled":false,"LockoutEnd":null,"LockoutEnabled":false,"AccessFailedCount":0}
""";
        SessionUser user = CurrentSession.StringToUser(inputString);

        Assert.Equal("Test Display", user.DisplayName);
        Assert.False(user.IdConfirmed);
        Assert.Equal(ContentRestrictions.Default, user.Restrictions);
        Assert.Null(user.SuspensionEnd);
        Assert.Equal((UInt128)Math.Pow(2, 64) + 100, user.Id);
    }

}


public class TokenStorageTests
{

    [Fact]
    public void Test_TokenStorageCreateEmpty()
    {
        TokenStorage tokens = new("");
        Assert.IsType<TokenStorage>(tokens);
        Assert.False(tokens.TryGetPrior("key", out string? value));
        Assert.Null(value);

        string export = tokens.ExportCurrent();
        Assert.Equal("{}", export);
    }

    [Fact]
    public void Test_TokenStorageCreateWithValues()
    {
        TokenStorage tokens = new("""{"testKey": "testValue"}""");
        Assert.True(tokens.TryGetPrior("testKey", out string? value));
        Assert.Equal("testValue", value);
    }

    [Fact]
    public void Test_TokenStorageSetsGets()
    {
        TokenStorage tokens = new("");
        tokens.SetCurrent("currentKey", "currentValue");
        tokens.SetPrior("priorKey", "priorValue");
        Assert.True(tokens.TryGetCurrent("currentKey", out string? value));
        Assert.Equal("currentValue", value);
        Assert.True(tokens.TryGetPrior("priorKey", out value));
        Assert.Equal("priorValue", value);
    }

    [Fact]
    public void Test_ExportImport()
    {
        TokenStorage tokens = new("");
        tokens.SetCurrent("currentKey", "currentValue");
        tokens.SetCurrent("currentKey2", "currentValue2");
        Assert.True(tokens.TryGetCurrent("currentKey", out string? value));
        Assert.Equal("currentValue", value);
        Assert.True(tokens.TryGetCurrent("currentKey2", out value));
        Assert.Equal("currentValue2", value);

        string export = tokens.ExportCurrent();

        TokenStorage newTokens = new("");
        newTokens.ImportPrior(export);
        Assert.True(newTokens.TryGetPrior("currentKey", out value));
        Assert.Equal("currentValue", value);
        Assert.True(newTokens.TryGetPrior("currentKey2", out value));
        Assert.Equal("currentValue2", value);
    }
}