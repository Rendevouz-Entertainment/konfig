using SpaceWarp.API.Mods;
using SpaceWarp;
using BepInEx;
using ShadowUtilityLIB;
using ShadowUtilityLIB.UI;
using Logger = ShadowUtilityLIB.logging.Logger;
using System.Text.RegularExpressions;
using KSP.Modules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace Konfig;
public abstract class PatchModule
{
    public abstract void Patch(dynamic Module, dynamic Data, Logger logger);
}

[BepInPlugin("com.shadow.konfig", "Konfig", "0.0.1")]
[BepInDependency(ShadowUtilityLIBMod.ModId, ShadowUtilityLIBMod.ModVersion)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]

public class KonfigMod : BaseSpaceWarpPlugin
{
    public static string ModId = "com.shadow.Konfig";
    public static string ModName = "Konfig";
    public static string ModVersion = "0.0.1";

    private static string LocationFile = Assembly.GetExecutingAssembly().Location;
    private static string LocationDirectory = Path.GetDirectoryName(LocationFile);

    public Dictionary<string,Type> PatchList = new Dictionary<string, Type>();

    private Logger logger = new Logger(ModName, ModVersion);
    public static Manager manager;

    public static bool IsDev = true;
    public override void OnInitialized()
    {
        GetConfigs();
        logger.Log("Initialized");
    }
    void Awake()
    {
        if (IsDev)
        {
            ShadowUtilityLIBMod.EnableDebugMode();
        }
        
    }
    void GetConfigs()
    {
        Regex regex = new Regex(@"^\[Target[(]\w+[)]]\n^\[Module[(]\w+[)]]\n^\[Data[(]\w+[)]]\n",RegexOptions.Multiline);
        
        try
        {
            logger.Log("Getting patches");
            foreach (string dir in Directory.EnumerateDirectories(Path.GetFullPath($@"{LocationDirectory}\..\")))
            {
                logger.Log($"Searching {dir}");
                foreach (string konfigLoc in Directory.EnumerateFiles(Path.GetFullPath($@"{dir}"), "*.konfig"))
                {
                    logger.Log($"Found patch file {konfigLoc}");
                    int patchID = 0;
                    string PatchData =  String.Join("\n" ,File.ReadAllLines(Path.GetFullPath($@"{konfigLoc}")));
                    string[] Patches = regex.Split(PatchData);
                    MatchCollection Patches_Headers = regex.Matches(PatchData);
                    logger.Debug(PatchData);
                    foreach (var patch in Patches)
                    {
                        var patchHeader = Patches_Headers[patchID].Value;
                        var targetStr = Regex.Match(patchHeader, @"\[Target[(](.*?)[)]\]").Value;
                        var moduleStr = Regex.Match(patchHeader, @"\[Module[(](.*?)[)]\]").Value;
                        var dataStr = Regex.Match(patchHeader, @"\[Data[(](.*?)[)]\]").Value;

                        logger.Debug(patch);

                        
                        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@$"
using KSP.Game;
using Konfig;
using Logger = ShadowUtilityLIB.logging.Logger;

namespace KPatcher;

public static class patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]} : PatchModule {{
    public static override Patch({moduleStr} Module, {dataStr} Data, Logger logger){{
        {patch}
    }}
}}
");
                        CSharpCompilation compilation = CSharpCompilation.Create($"Patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]}_PatchModule", new [] {syntaxTree});
                        
                        using (var ms = new MemoryStream())
                        {
                            EmitResult result = compilation.Emit(ms);
                            if (!result.Success)
                            {
                                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                                    diagnostic.IsWarningAsError ||
                                    diagnostic.Severity == DiagnosticSeverity.Error);

                                foreach (Diagnostic diagnostic in failures)
                                {
                                    logger.Error($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                                }
                            }
                            ms.Seek(0, SeekOrigin.Begin);
                            Assembly assembly = Assembly.Load(ms.ToArray());
                            Type type = assembly.GetType();
                            object obj = Activator.CreateInstance(type);
                            
                            type.InvokeMember("Patch", BindingFlags.InvokeMethod,
                                null,
                                obj,
                                new object[] { new Module_Engine(), new Data_Engine() , logger });
                            //var x = GameManager.Instance.Game.Parts.Get("");
                            //x.data
                            PatchList.Add($"patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]}", type);
                        }


                        patchID++;
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
        }
    }
}