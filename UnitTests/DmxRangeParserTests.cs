
using Xunit;
namespace MDMX_MaskCreator.UnitTests;
using System.Linq;
public class DmxRangeParserTests
{
    // --- Parse count ---

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var result = DmxRangeParser.Parse("");
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmpty()
    {
        var result = DmxRangeParser.Parse("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_SingleRange_ReturnsOneRange()
    {
        var result = DmxRangeParser.Parse("U1:C1-C50");
        Assert.Single(result);
    }

    [Fact]
    public void Parse_MultipleRanges_ReturnsAll()
    {
        var result = DmxRangeParser.Parse("U1:C1-C50, U3:C10-C20, U1:C1-U2:C23");
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Parse_InvalidInput_SkipsGracefully()
    {
        var result = DmxRangeParser.Parse("garbage, U1:C1-C10, moreubbish");
        Assert.Single(result);
    }

    // --- Single universe range ---

    [Fact]
    public void Parse_SingleUniverseRange_CorrectStart()
    {
        var result = DmxRangeParser.Parse("U1:C1-C50");
        Assert.Equal(new DmxAddress(1, 1), result[0].Start);
    }

    [Fact]
    public void Parse_SingleUniverseRange_CorrectEnd()
    {
        var result = DmxRangeParser.Parse("U1:C1-C50");
        Assert.Equal(new DmxAddress(1, 50), result[0].End);
    }

    [Fact]
    public void Parse_SingleUniverseRange_CaseInsensitive()
    {
        var result = DmxRangeParser.Parse("u1:c1-c50");
        Assert.Single(result);
        Assert.Equal(new DmxAddress(1, 1), result[0].Start);
    }

    // --- Cross universe range ---

    [Fact]
    public void Parse_CrossUniverseRange_CorrectStart()
    {
        var result = DmxRangeParser.Parse("U1:C1-U2:C23");
        Assert.Equal(new DmxAddress(1, 1), result[0].Start);
    }

    [Fact]
    public void Parse_CrossUniverseRange_CorrectEnd()
    {
        var result = DmxRangeParser.Parse("U1:C1-U2:C23");
        Assert.Equal(new DmxAddress(2, 23), result[0].End);
    }

    // --- Single channel ---

    [Fact]
    public void Parse_SingleChannel_StartEqualsEnd()
    {
        var result = DmxRangeParser.Parse("U1:C5");
        Assert.Equal(result[0].Start, result[0].End);
        Assert.Equal(new DmxAddress(1, 5), result[0].Start);
    }

    // --- Expand ---

    [Fact]
    public void Expand_SingleChannel_ReturnsOneAddress()
    {
        var range = new DmxRange(new DmxAddress(1, 1), new DmxAddress(1, 1));
        var expanded = range.Expand().ToList();
        Assert.Single(expanded);
        Assert.Equal(new DmxAddress(1, 1), expanded[0]);
    }

    [Fact]
    public void Expand_SameUniverseRange_ReturnsCorrectCount()
    {
        var range = new DmxRange(new DmxAddress(1, 1), new DmxAddress(1, 6));
        var expanded = range.Expand().ToList();
        Assert.Equal(6, expanded.Count);
    }

    [Fact]
    public void Expand_SameUniverseRange_CorrectAddresses()
    {
        var range = new DmxRange(new DmxAddress(1, 1), new DmxAddress(1, 3));
        var expanded = range.Expand().ToList();

        Assert.Equal(new DmxAddress(1, 1), expanded[0]);
        Assert.Equal(new DmxAddress(1, 2), expanded[1]);
        Assert.Equal(new DmxAddress(1, 3), expanded[2]);
    }

    [Fact]
    public void Expand_CrossUniverseRange_SpansCorrectly()
    {
        // U1:C511 - U2:C2 = 4 channels (511, 512, 1, 2)
        var range = new DmxRange(new DmxAddress(1, 511), new DmxAddress(2, 2));
        var expanded = range.Expand().ToList();

        Assert.Equal(4, expanded.Count);
        Assert.Equal(new DmxAddress(1, 511), expanded[0]);
        Assert.Equal(new DmxAddress(1, 512), expanded[1]);
        Assert.Equal(new DmxAddress(2, 1),   expanded[2]);
        Assert.Equal(new DmxAddress(2, 2),   expanded[3]);
    }

    [Fact]
    public void Expand_CrossUniverseRange_StartsAtUniverseBoundary()
    {
        // U1:C512 - U2:C1 = exactly the boundary
        var range = new DmxRange(new DmxAddress(1, 512), new DmxAddress(2, 1));
        var expanded = range.Expand().ToList();

        Assert.Equal(2, expanded.Count);
        Assert.Equal(new DmxAddress(1, 512), expanded[0]);
        Assert.Equal(new DmxAddress(2, 1),   expanded[1]);
    }

    [Fact]
    public void Expand_WithSpacesInInput_ParsesCorrectly()
    {
        var result = DmxRangeParser.Parse("U1:C1-C10 , U2:C1-C10");
        Assert.Equal(2, result.Count);
    }
}