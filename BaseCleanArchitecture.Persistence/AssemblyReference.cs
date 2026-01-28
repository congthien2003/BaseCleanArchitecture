using System.Reflection;

namespace BaseCleanArchitecture.Persistence;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}

