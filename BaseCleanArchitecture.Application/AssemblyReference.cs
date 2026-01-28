using System.Reflection;

namespace BaseCleanArchitecture.Application;
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
