using System.Security.Cryptography;

namespace Utilities;

public static class Utils
{
    public static UInt128 CreateItemID()
    {
        byte[] random = RandomNumberGenerator.GetBytes(8);
        return CreateItemID(random);
    }

    public static UInt128 CreateItemID(byte[] random)
    {
        UInt128 unixMilliStamp = (UInt128)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        UInt128 RandomInt = BitConverter.ToUInt64(random);
        unixMilliStamp <<= 80; // NOTE: bits 48-63 are left as "0" to allow for future expansion
        return (unixMilliStamp | RandomInt);
    }

    public static byte[] GetU128Bytes(UInt128 num)
    {
        byte[] bytes = BitConverter.GetBytes(num);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    public static UInt128 GetU128Val(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt128(bytes);
    }
}