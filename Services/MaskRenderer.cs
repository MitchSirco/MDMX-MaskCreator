using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using CsvHelper.Configuration.Attributes;
using MDMX_MaskCreator.Services;
using SkiaSharp;


namespace MDMX_MaskCreator.Grid;

public class MaskRenderer
{
    public const int GridWidth = 2560; // TODO have this be editable
    public const int GridHeight = 208;
    
    private readonly int _gridWidth;
    
    public MaskRenderer(int gridWidth = GridWidth)
    {
        _gridWidth = gridWidth;
    }
    
    public SKBitmap Render(
        IEnumerable<PatchedFixture> fixturesToMask, 
        IEnumerable<DmxRange> dmxRanges, 
        bool invertMask = false,
        bool fullColumnForcesWhiteCrc = true)
    {
        // L8 - 8-bit grayscale
        SKBitmap bitmap  = new SKBitmap(_gridWidth, GridHeight, SKColorType.Gray8, SKAlphaType.Opaque); 
        
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black); // always start black
        
        using var whitePaint = new SKPaint { Color = SKColors.White };
        using var blackPaint = new SKPaint { Color = SKColors.Black };
        
        var touchedColumns = new HashSet<int>();
        
        foreach (var fixture in fixturesToMask)
        {
            var slots = GridLayout.ResolveSlots(fixture);
            foreach (var (column, slot) in slots)
            {
                var rect = GridLayout.ToPixelRect(column, slot);
                if (rect.Right > _gridWidth)
                    continue; // slot falls outside grid bounds, skip silently
                
                canvas.DrawRect(rect, whitePaint);
                touchedColumns.Add(column);

            }
        }
        
        // draw DMX range selections
        foreach (var range in dmxRanges)
        {
            foreach (var address in range.Expand())
            {
                var absoluteSlot = GridLayout.ToAbsoluteSlot(address.Universe, address.Channel);
                var (column, slot) = GridLayout.ToColumnSlot(absoluteSlot);
                var rect = GridLayout.ToPixelRect(column, slot);
                if (rect.Right > _gridWidth) continue;
                canvas.DrawRect(rect, whitePaint);
                touchedColumns.Add(column);
            }
        }
        
        
        canvas.Flush();

        
        // recompute CRC for every touched column
        foreach (var column in touchedColumns)
        {
            // first clear the existing parity region for this column
            int x = column * GridLayout.ColumnWidth;
            int yStart = GridLayout.SlotsPerColumn * GridLayout.SlotHeight;
            
            // clear parity region to black
            canvas.DrawRect(x, yStart, GridLayout.ColumnWidth,
                GridLayout.ParityBits * GridLayout.BitSize, blackPaint);
            
            // read back the 6 bytes and compute CRC
            // flush canvas before reading pixels back
            canvas.Flush();
            
            // read back bytes and compute CRC
            var bytes = ReadColumnBytes(bitmap, column);
            
            if (fullColumnForcesWhiteCrc)
            {
                // all slots active — force entire parity region white
                canvas.DrawRect(x, yStart, GridLayout.ColumnWidth,
                    GridLayout.ParityBits * GridLayout.BitSize, whitePaint);
            }
            else
            {
                // partial column — use true CRC value
                var crc = CrcCalculator.Compute(bytes);
                DrawCrc(canvas, blackPaint, whitePaint, column, crc);
            }
        }
        
        // This is slow, i bet there is a better way to do this
        if (invertMask)
        {
            var invertedBitmap =
                new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var canvass = new SKCanvas(invertedBitmap);
            using var paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(
                    SKColors.White, 
                    SKBlendMode.Difference)
            };
            canvass.DrawBitmap(bitmap, 0, 0, paint);
            canvass.Flush();
            
            var testPixel = invertedBitmap.GetPixel(0, 0);
            Console.WriteLine($"Inverted pixel at 0,0: R={testPixel.Red}");
            bitmap.Dispose();
            return invertedBitmap;
        }
        
        
        return bitmap;
    }

    private static bool IsColumnFullyActive(byte[] bytes)
        => bytes.All(b => b == 255);

    
    public SKBitmap Render(IEnumerable<PatchedFixture> fixturesToMask, bool invertMask = false)
        => Render(fixturesToMask, [], invertMask);
    
    private static byte[] ReadColumnBytes(SKBitmap bitmap, int column)
    {
        var bytes = new byte[6];

        for (int slot = 0; slot < 6; slot++)
        {
            byte value = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                int x = column * GridLayout.ColumnWidth;
                int y = slot * GridLayout.SlotHeight + bit * GridLayout.BitSize;
                var pixel = bitmap.GetPixel(x, y);

                if (pixel.Red == 255) // white = 1
                {
                    int nth = 7 - bit;
                    value |= (byte)(1 << nth);
                }
            }
            bytes[slot] = value;
        }

        return bytes;
    }
    
    private static SKBitmap MakeBlackTransparent(SKBitmap bitmap)
    {
        var result = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        for (int y = 0; y < bitmap.Height; y++)
        for (int x = 0; x < bitmap.Width; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            result.SetPixel(x, y, pixel.Red == 0
                ? SKColors.Transparent
                : pixel);
        }

        return result;
    }
    
    private static void DrawCrc(
        SKCanvas canvas,
        SKPaint blackPaint,
        SKPaint whitePaint,
        int column,
        byte crc)
    {
        int x = column * GridLayout.ColumnWidth;
        int yStart = GridLayout.SlotsPerColumn * GridLayout.SlotHeight; // 192px

        for (int bit = 0; bit < 4; bit++)
        {
            // MSB first, top to bottom — bit 3 at top, bit 0 at bottom
            uint mask = (uint)(1 << (3 - bit));
            bool isOn = (crc & mask) != 0;

            int y = yStart + bit * GridLayout.BitSize;
            canvas.DrawRect(x, y, GridLayout.ColumnWidth, GridLayout.BitSize,
                isOn ? whitePaint : blackPaint);

        }
    }
    
    public void SaveAsPng(SKBitmap bitmap, string path)     {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    public void RenderAndSave(
        IEnumerable<PatchedFixture> fixturesToMask,
        IEnumerable<DmxRange> dmxRanges,
        string path,
        bool invertMask = false,
        bool blackAsTransparent = false)
    {
        using var bitmap = Render(fixturesToMask, dmxRanges, invertMask);
        if (blackAsTransparent)
        {
            using var transparent = MakeBlackTransparent(bitmap);
            SaveAsPng(transparent, path);
        }
        else
            SaveAsPng(bitmap, path);
    }
    
    public void RenderAndSaveAs169(
        IEnumerable<PatchedFixture> fixturesToMask,
        IEnumerable<DmxRange> dmxRanges,
        string path,
        int gridWidth,
        bool invertMask = false,
        bool blackAsTransparent = false,
        bool whitePadding = false,
        bool fullColumnForcesWhiteCrc = true)
    {
        var height169 = gridWidth * 9 / 16;
        using var fullBitmap = new SKBitmap(gridWidth, height169, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var fullCanvas = new SKCanvas(fullBitmap);
        
        fullCanvas.Clear(whitePadding ? SKColors.White : SKColors.Black);

        using var maskBitmap = Render(fixturesToMask, dmxRanges, invertMask, fullColumnForcesWhiteCrc);
        fullCanvas.DrawBitmap(maskBitmap, 0, 0);

        fullCanvas.Flush();

        if (blackAsTransparent)
        {
            using var transparent = MakeBlackTransparent(fullBitmap);
            SaveAsPng(transparent, path);
        }
        else
        {
            SaveAsPng(fullBitmap, path);
        }
    }
    
    public SKBitmap RenderPatchLayoutOverlay(
        List<PatchedFixture> allFixtures,
        List<PatchedFixture> selectedFixtures,
        List<DmxRange> dmxRanges,
        bool invertMask = false,
        bool fullColumnForcesWhiteCrc = true,
        int seed = 0)
    {
        var bitmap = new SKBitmap(_gridWidth, GridHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        

        var colorMap = FixtureColorAssigner.AssignColors(allFixtures.Select(f => f.Name), seed);

        foreach (var fixture in allFixtures)
        {
            var color = colorMap[fixture.Name];
            using var paint = new SKPaint { Color = color };
            var slots = GridLayout.ResolveSlots(fixture);
            foreach (var (column, slot) in slots)
            {
                var rect = GridLayout.ToPixelRect(column, slot);
                if (rect.Right > _gridWidth) continue;
                canvas.DrawRect(rect, paint);
            }
        }

// overlay mask
        using var maskBitmap = Render(selectedFixtures, dmxRanges, invertMask, fullColumnForcesWhiteCrc);

        SKBlendMode blendMode = invertMask ? SKBlendMode.Multiply : SKBlendMode.Screen;
        SKColor overlayColor = invertMask
            ? new SKColor(0, 0, 0, 230)
            : new SKColor(255, 255, 255, 180);

        using var overlayPaint = new SKPaint
        {
            Color = overlayColor,
            BlendMode = blendMode
        };
        canvas.DrawBitmap(maskBitmap, 0, 0, overlayPaint);

        // draw universe boundary markers
        using var linePaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 120),
            StrokeWidth = 1,
            IsStroke = true
        };
        using var textPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 230),
            BlendMode = SKBlendMode.Difference,
            TextSize = 18,
            IsAntialias = true
        };

// figure out how many universes fit in the grid
        int maxUniverses = (_gridWidth / GridLayout.ColumnWidth * GridLayout.SlotsPerColumn) / 512 + 2;

        for (int u = 1; u <= maxUniverses; u++)
        {
            int absoluteSlot = (u - 1) * 512;
            int column = absoluteSlot / GridLayout.SlotsPerColumn;
            int x = column * GridLayout.ColumnWidth;

            if (x >= _gridWidth) break;

            // vertical line
            canvas.DrawLine(x, 0, x, GridHeight, linePaint);

            // label at top
            canvas.DrawText($"U{u}", x + 4, 20, textPaint);
        }
        
        return bitmap;
    }


}