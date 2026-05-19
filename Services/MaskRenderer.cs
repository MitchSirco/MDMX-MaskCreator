using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
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

    
    public SKBitmap Render(IEnumerable<PatchedFixture> fixturesToMask, bool invertMask = false)
    {
        // L8 - 8-bit grayscale
        SKBitmap bitmap  = new SKBitmap(_gridWidth, GridHeight, SKColorType.Gray8, SKAlphaType.Opaque); 
        
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black); // always start black
        
        using var paint = new SKPaint { Color = SKColors.White }; // selected = white

        var touchedColumns = new HashSet<int>();
        
        foreach (var fixture in fixturesToMask)
        {
            var slots = GridLayout.ResolveSlots(fixture);
            foreach (var (column, slot) in slots)
            {
                var rect = GridLayout.ToPixelRect(column, slot);
                if (rect.Right > _gridWidth)
                    continue; // slot falls outside grid bounds, skip silently
                
                canvas.DrawRect(rect, paint);
                touchedColumns.Add(column);

            }
        }
        
        canvas.Flush();
        
        using var blackPaint = new SKPaint { Color = SKColors.Black };
        using var whitePaint = new SKPaint { Color = SKColors.White };

        
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
            var crc = CrcCalculator.Compute(bytes);
            DrawCrc(canvas, blackPaint, whitePaint, column, crc);
        }
        
        if (!invertMask)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    bitmap.SetPixel(x, y, new SKColor(
                        (byte)(255 - pixel.Red),
                        (byte)(255 - pixel.Green),
                        (byte)(255 - pixel.Blue),
                        255
                    ));
                }
            }
        }
        
        
        return bitmap;
    }

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
        string path,
        bool invertMask = false)
    {
        using var bitmap = Render(fixturesToMask, invertMask);
        SaveAsPng(bitmap, path);
    }
    
    public void RenderAndSaveAs169(
        IEnumerable<PatchedFixture> fixturesToMask,
        string path,
        int gridWidth,
        bool invertMask = false)
    {
        var height169 = gridWidth * 9 / 16;

        using var fullBitmap = new SKBitmap(gridWidth, height169, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var fullCanvas = new SKCanvas(fullBitmap);
        fullCanvas.Clear(SKColors.Black);

        using var maskBitmap = Render(fixturesToMask, invertMask);
        fullCanvas.DrawBitmap(maskBitmap, 0, 0);

        using var image = SKImage.FromBitmap(fullBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }


}