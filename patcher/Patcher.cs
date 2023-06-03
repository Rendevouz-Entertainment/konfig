using Konfig;
using Mono.Cecil;

namespace KonfigPatcher;

public static class Patcher
{
    private static readonly string BasePath = Path.Combine("BepInEx", "plugins", "Konfig", "lib");

    public static IEnumerable<string> TargetDLLs { get; } = new[]
    {
        "Microsoft.CodeAnalysis.CSharp.dll",
        "Microsoft.CodeAnalysis.dll",
        "System.Collections.Immutable.dll",
        "System.Memory.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Numerics.Vectors.dll",
        "System.Reflection.Metadata.dll",
        "System.Threading.Tasks.Extensions.dll"
    };

    public static void Patch(ref AssemblyDefinition assembly)
    {
        assembly = AssemblyDefinition.ReadAssembly(Path.Combine(BasePath, $"{assembly.Name.Name}.dll"));
    }
}