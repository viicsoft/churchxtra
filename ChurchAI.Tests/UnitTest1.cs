using Xunit;
using System;

namespace ChurchAI.Tests;

public class UnitTest1
{
    [Fact]
    public void TestQrCodeGeneration()
    {
        Exception? error = null;
        byte[]? qrCodeBytes = null;
        try
        {
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode("http://localhost:8090/", QRCoder.QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
            {
                qrCodeBytes = qrCode.GetGraphic(20);
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }

        Assert.Null(error);
        Assert.NotNull(qrCodeBytes);
        Assert.NotEmpty(qrCodeBytes);
    }
}
