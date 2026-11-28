using NyangoPress.Application.Orders;
using QRCoder;

namespace NyangoPress.Infrastructure.Orders;

public sealed class QrCodeService : IQrCodeService
{
    public string GenerateSvg(string payload, int pixelsPerModule = 4)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var qr = new SvgQRCode(data);
        return qr.GetGraphic(pixelsPerModule, "#0B3C5D", "#FFFFFF", drawQuietZones: true);
    }

    public byte[] GeneratePng(string payload, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }
}
