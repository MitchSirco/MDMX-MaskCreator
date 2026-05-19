using System.Collections;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
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

    
    public SKBitmap Render(IEnumerable<PatchedFixture> fixturesToMask)
    {
        // L8 - 8-bit grayscale
        SKBitmap bitmap  = new SKBitmap(_gridWidth, GridHeight, SKColorType.Gray8, SKAlphaType.Opaque); 
        
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        
        using var paint = new SKPaint { Color = SKColors.Black };

        foreach (var fixture in fixturesToMask)
        {
            var slots = GridLayout.ResolveSlots(fixture);
            foreach (var (column, slot) in slots)
            {
                var rect = GridLayout.ToPixelRect(column, slot);
                if (rect.Right > _gridWidth)
                    continue; // slot falls outside grid bounds, skip silently
                
                canvas.DrawRect(rect, paint);
            }
        }
        
        return bitmap;
    }

    public void SaveAsPng(SKBitmap bitmap, string path)     {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }

    public void RenderAndSave(IEnumerable<PatchedFixture> fixturesToMask, string path)
    {
        using var bitmap = Render(fixturesToMask);
        SaveAsPng(bitmap, path);
    }

}