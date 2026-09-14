using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@""C:\Users\USER\.nuget\packages\ndilibdotnetcorebase\2024.7.22.1\lib\net6.0\NDILibDotNetCore.dll"");
        var type = asm.GetType(""NewTek.NDI.VideoFrame"");
        if (type == null) {
            Console.WriteLine(""VideoFrame not found"");
        } else {
            foreach(var member in type.GetMembers())
            {
                Console.WriteLine(member.Name);
            }
        }
    }
}
