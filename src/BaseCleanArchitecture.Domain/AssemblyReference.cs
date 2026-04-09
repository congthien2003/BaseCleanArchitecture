using System.Reflection;

namespace BaseCleanArchitecture.Domain;
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}