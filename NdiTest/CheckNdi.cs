using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@""C:\Users\USER\.nuget\packages\ndilibdotnetcorebase\2024.7.22.1\lib\net8.0\NDILibDotNetCoreBase.dll"");
        var types = asm.GetTypes();
        var type = types.FirstOrDefault(t => t.Name == ""VideoFrame"");
        if (type == null) {
            Console.WriteLine(""VideoFrame not found"");
            foreach (var t in types) {
                if (t.Name.Contains(""Video"")) Console.WriteLine(t.FullName);
            }
            return;
        }
        foreach(var c in type.GetConstructors()) {
            Console.WriteLine(c);
        }
        var m = type.GetMethod(""Dispose"", BindingFlags.Public | BindingFlags.Instance);
        var body = m.GetMethodBody();
        Console.WriteLine(""Dispose size: "" + body.GetILAsByteArray().Length);
    }
}
