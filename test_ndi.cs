using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@""C:\Users\USER\.nuget\packages\ndilibdotnetcorebase\2024.7.22.1\lib\net6.0\NDILibDotNetCore.dll"");
        foreach(var type in asm.GetTypes())
        {
            if (type.IsPublic) Console.WriteLine(type.FullName);
        }
    }
}
