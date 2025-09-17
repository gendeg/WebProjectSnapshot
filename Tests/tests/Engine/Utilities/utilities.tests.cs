using Utilities;

namespace UtilitiesTests;

public class UtilsTests
{

    [Fact]
    public void Test_GetU128Bytes()
    {
        UInt128 num = 5643829573642;
        byte[] bytes = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x22, 0x0E, 0x74, 0xF8, 0x0A];
        Assert.Equal(bytes, Utils.GetU128Bytes(num));
    }

    [Fact]
    public void Test_GetU128Val()
    {
        UInt128 num = 5643829573642;
        byte[] bytes = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x22, 0x0E, 0x74, 0xF8, 0x0A];
        Assert.Equal(num, Utils.GetU128Val(bytes));
    }

    [Fact]
    public void Test_CreateItemIDTimestamp()
    {
        UInt128 ID = Utils.CreateItemID();
        UInt128 now = (UInt128)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        UInt128 past = (now - 100000) << 80;
        UInt128 future = (now + 100000) << 80;

        Assert.True(past < ID);
        Assert.True(future > ID);
    }

}