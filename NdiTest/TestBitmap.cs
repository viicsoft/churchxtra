using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class Program
{
    [STAThread]
    static void Main()
    {
        var app = new Application();
        Task.Run(() => {
            try {
                int width = 800, height = 600;
                var pixels = new byte[width * height * 4];
                var format = PixelFormats.Pbgra32;
                var stride = width * 4;
                var bitmap = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
                
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var ms = new MemoryStream()) {
                    encoder.Save(ms);
                    Console.WriteLine(""Success: "" + ms.Length);
                }
            } catch(Exception e) {
                Console.WriteLine(""Error: "" + e.Message);
            }
            Environment.Exit(0);
        });
        app.Run();
    }
}
