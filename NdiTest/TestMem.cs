using System;
using System.Runtime.InteropServices;
using NewTek;
using NewTek.NDI;

class Program
{
    static void Main()
    {
        var buffer = Marshal.AllocHGlobal(1024);
        Console.WriteLine(""Allocated"");
        var frame = new VideoFrame(buffer, 10, 10, 40, NDIlib.FourCC_type_e.FourCC_type_BGRA, 1.0f, 30000, 1000, NDIlib.frame_format_type_e.frame_format_type_progressive);
        Console.WriteLine(""Created"");
        frame.Dispose();
        Console.WriteLine(""Disposed"");
        
        // Let's try to write to the buffer. If it was freed, it might crash, but usually writing to freed memory in Windows doesn't crash immediately unless it's unmapped.
        // We can just free it again. If it was already freed, FreeHGlobal might throw or crash.
        // Actually, we can use a pinned array.
        var arr = new byte[1024];
        var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
        try {
            var frame2 = new VideoFrame(handle.AddrOfPinnedObject(), 10, 10, 40, NDIlib.FourCC_type_e.FourCC_type_BGRA, 1.0f, 30000, 1000, NDIlib.frame_format_type_e.frame_format_type_progressive);
            frame2.Dispose();
            Console.WriteLine(""Disposed frame 2"");
        } catch(Exception e) {
            Console.WriteLine(""Error: "" + e);
        } finally {
            handle.Free();
        }
    }
}
