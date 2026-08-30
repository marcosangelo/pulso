using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace Pulso.Link;

public static class QrPng
{
    public static BitmapImage Render(string payload, int pixelsPerModule = 8)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(pixelsPerModule, [0xE8, 0xED, 0xF7, 0xFF], [0x0B, 0x10, 0x20, 0xFF], false);
        var image = new BitmapImage();
        using var ms = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
