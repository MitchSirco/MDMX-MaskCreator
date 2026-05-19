using System;

namespace MDMX_MaskCreator.Services;

public class CrcCalculator
{
    private const int Poly = 0x03;

    public static byte Compute(byte[] data)
    {
        if (data.Length != 6)
            throw new ArgumentException("CRC requires exactly 6 bytes");

        uint crc = 0;
        uint poly = 0x03;

        for (int i = 0; i < 6; i++)
        {
            byte v = data[i];
            for (int bit = 7; bit >= 0; bit--)
            {
                uint inBit = (uint)(v >> bit) & 1;
                uint top = (crc >> 3) & 1;
                crc = ((crc << 1) | inBit) & 0xF;
                if (top == 1)
                    crc ^= poly;
            }
        }

        return (byte)crc;
    }
    
    public static byte ComputeDebug(byte[] data)
    {
        uint crc = 0;
        uint poly = 0x03;

        for (int i = 0; i < 6; i++)
        {
            byte v = data[i];
            for (int bit = 7; bit >= 0; bit--)
            {
                uint inBit = (uint)(v >> bit) & 1;
                uint top = (crc >> 3) & 1;
                crc = ((crc << 1) | inBit) & 0xF;
                if (top == 1) crc ^= poly;
            }
            Console.WriteLine($"After byte {i} (value={data[i]}): crc={crc} binary={Convert.ToString(crc, 2).PadLeft(4, '0')}");
        }

        return (byte)crc;
    }

}