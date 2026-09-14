using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@""C:\Users\USER\.nuget\packages\ndilibdotnetcorebase\2024.7.22.1\lib\net8.0\NDILibDotNetCoreBase.dll"");
        var type = asm.GetType(""NewTek.NDI.VideoFrame"");
        var method = type.GetMethod(""Dispose"", BindingFlags.Public | BindingFlags.Instance);
        Console.WriteLine(""Dispose exists: "" + (method != null));
        
        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach(var m in methods) {
            if (m.Name.Contains(""Dispose"")) Console.WriteLine(""NonPublic: "" + m.Name);
        }
    }
}
