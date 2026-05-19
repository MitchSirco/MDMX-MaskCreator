using System;
using MDMX_MaskCreator.Services;
using Xunit;

namespace MDMX_MaskCreator.UnitTests;

public class CrcCalculatorTests
{
    // helper — builds a 6-byte array where the first N slots are 0xFF, rest 0x00
    private static byte[] Bytes(params byte[] values)
    {
        var result = new byte[6];
        for (int i = 0; i < values.Length && i < 6; i++)
            result[i] = values[i];
        return result;
    }

    [Fact]
    public void Crc_C1Only_255_Is5()
    {
        var data = Bytes(255, 0, 0, 0, 0, 0);
        Assert.Equal(5, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C1C2_255_Is4()
    {
        var data = Bytes(255, 255, 0, 0, 0, 0);
        Assert.Equal(4, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C1C3_255_Is15()
    {
        var data = Bytes(255, 255, 255, 0, 0, 0);
        Assert.Equal(15, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C1C4_255_Is6()
    {
        var data = Bytes(255, 255, 255, 255, 0, 0);
        Assert.Equal(6, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C1C5_255_Is10()
    {
        var data = Bytes(255, 255, 255, 255, 255, 0);
        Assert.Equal(10, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C1C6_255_Is7()
    {
        var data = Bytes(255, 255, 255, 255, 255, 255);
        Assert.Equal(7, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C2C6_255_Is2()
    {
        var data = Bytes(0, 255, 255, 255, 255, 255);
        Assert.Equal(2, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C3C6_255_Is3()
    {
        var data = Bytes(0, 0, 255, 255, 255, 255);
        Assert.Equal(3, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C4C6_255_Is8()
    {
        var data = Bytes(0, 0, 0, 255, 255, 255);
        Assert.Equal(8, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C5C6_255_Is1()
    {
        var data = Bytes(0, 0, 0, 0, 255, 255);
        Assert.Equal(1, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_C6Only_255_Is13()
    {
        var data = Bytes(0, 0, 0, 0, 0, 255);
        Assert.Equal(13, CrcCalculator.Compute(data));
    }

    [Fact]
    public void Crc_AllZero_Is0()
    {
        var data = Bytes(0, 0, 0, 0, 0, 0);
        Assert.Equal(0, CrcCalculator.Compute(data));
    }
    
    [Fact]
    public void Crc_C5C6_Debug()
    {
        var data = new byte[] { 0, 0, 0, 0, 255, 255 };
        var result = CrcCalculator.ComputeDebug(data);
        Console.WriteLine($"Final CRC: {result} = {Convert.ToString(result, 2).PadLeft(4, '0')}");
    }

    [Fact]
    public void Crc_RequiresExactly6Bytes()
    {
        Assert.Throws<ArgumentException>(() => CrcCalculator.Compute(new byte[5]));
        Assert.Throws<ArgumentException>(() => CrcCalculator.Compute(new byte[7]));
    }
}