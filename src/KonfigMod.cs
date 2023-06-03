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
using UnityEngine;
using Newtonsoft.Json;
using KSP.Game;
using KSP.Sim.Definitions;
using KSP.Messages;
using KSP.UI.Binding;

namespace Konfig;
public abstract class PatchModule<M,D>
{
    
    public abstract void Patch(M Module, D Data, string partName,PartData partData, PartCore Target);
}
public class PatchListData
{
    public string ModuleName { get; set; }
    public string DataName { get; set; }
    public Type PatchType { get; set; }
    public PatchListData(string mn,string dn,Type pt)
    {
        ModuleName = mn;
        DataName = dn;
        PatchType = pt;
    }
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

    public bool patchRan = false;
    public Dictionary<string, List<PatchListData>> PatchList = new Dictionary<string, List<PatchListData>>();

    private Logger logger = new Logger(ModName, ModVersion);
    public static Manager manager;

    public static bool IsDev = true;
    public override void OnInitialized()
    {
        GetConfigs();
        GameManager.Instance.Game.Messages.Subscribe<GameStateChangedMessage>(GameStateChanged);
        logger.Log("Initialized");
        
    }
    void Awake()
    {
        if (IsDev)
        {
            ShadowUtilityLIBMod.EnableDebugMode();
        }
        
    }
    void GameStateChanged(MessageCenterMessage messageCenterMessage)
    {
        GameStateChangedMessage gameStateChangedMessage = messageCenterMessage as GameStateChangedMessage;
        if(gameStateChangedMessage.CurrentState == GameState.Loading && patchRan == false) {
            RunPatchers();
            patchRan = true;
        }
        

    }
    void RunPatchers()
    {
        try
        {
            foreach(string partPatches in PatchList.Keys)
            {
                PartCore SelectedPartToPatch = GameManager.Instance.Game.Parts.Get(partPatches);
                foreach(PatchListData PatchType in PatchList[partPatches])
                {
                    var partModule = SelectedPartToPatch.data.serializedPartModules.Find(partModule => partModule.Name == $"PartComponent{PatchType.ModuleName}");
                    var partData = partModule.ModuleData.Find(partData => partData.Name == PatchType.DataName);
                    object obj = Activator.CreateInstance(PatchType.PatchType);
                    var m = PatchType.PatchType.GetMethod("Patch");
                    m.Invoke(obj, new object[] { null, partData.DataObject , partPatches , SelectedPartToPatch.data , SelectedPartToPatch });
                }
            }
        }
        catch (Exception e)
        {
            logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
        }
    }
    void GetConfigs()
    {
        Regex regex = new Regex(@"^\[Target[(]\w+[)]]\n^\[Module[(]\w+[)]]\n^\[Data[(]\w+[)]]\n",RegexOptions.Multiline);
        
        try
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (var assembalyData in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if(assembalyData.Location == null || assembalyData.Location == "" || assembalyData.Location == " ")
                    {

                    }
                    else
                    {
                        references.Add(MetadataReference.CreateFromFile(assembalyData.Location));
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
                }
                
            }
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
                    Patches = Patches.Where(w => w != "").ToArray();
                    foreach (var patch in Patches)
                    {
                        var patchHeader = Patches_Headers[patchID].Value;
                        var targetStr = Regex.Match(patchHeader, @"[(](.*?)[)]", RegexOptions.Multiline).Groups[0].Value;
                        var moduleStr = Regex.Match(patchHeader, @"[(](.*?)[)]", RegexOptions.Multiline).NextMatch().Groups[0].Value;
                        var dataStr = Regex.Match(patchHeader, @"[(](.*?)[)]", RegexOptions.Multiline).NextMatch().NextMatch().Groups[0].Value;
                        targetStr = targetStr.Replace("(", "");
                        targetStr = targetStr.Replace(")", "");
                        moduleStr = moduleStr.Replace("(", "");
                        moduleStr = moduleStr.Replace(")", "");
                        dataStr = dataStr.Replace("(", "");
                        dataStr = dataStr.Replace(")", "");
                        logger.Debug(patch);

                        logger.Debug($$"""
using KSP.Game;
using KSP.Modules;
using Konfig;
using Logger = ShadowUtilityLIB.logging.Logger;

namespace KPatcher;

public class patch_{{targetStr}}_{{dir.Split('\\')[dir.Split('\\').Length - 1]}} : PatchModule<{{moduleStr}},{{dataStr}}> {
    private Logger logger = new Logger("Konfig Patch", "0.0.1");
    static void Main()
    {

    }
    public override void Patch({{moduleStr}} Module, {{dataStr}} Data, String partName,PartData partData, PartCore Target){
        {{patch}}
    }
}
""");
                        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText($$"""
using KSP.Game;
using KSP.Modules;
using KSP.Sim.Definitions;
using Konfig;
using Logger = ShadowUtilityLIB.logging.Logger;

namespace KPatcher;

public class patch_{{targetStr}}_{{dir.Split('\\')[dir.Split('\\').Length - 1]}} : PatchModule<{{moduleStr}},{{dataStr}}> {
    private Logger logger = new Logger("Konfig Patch", "0.0.1");
    static void Main()
    {

    }
    public override void Patch({{moduleStr}} Module, {{dataStr}} Data, string  partName,PartData partData, PartCore Target){
        {{patch}}
    }
}
""");
                        

                        CSharpCompilation compilation = CSharpCompilation.Create($"Patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]}_PatchModule", new [] {syntaxTree}, references);
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
                            Type type = assembly.GetType($"KPatcher.patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]}");
                            
                            //var x = GameManager.Instance.Game.Parts.Get("");
                            //x.data
                            if (PatchList.ContainsKey(targetStr))
                            {
                                PatchList[targetStr].Add(new PatchListData(moduleStr, dataStr,type));
                            }
                            else
                            {
                                PatchList.Add(targetStr, new List<PatchListData>());
                                PatchList[targetStr].Add(new PatchListData(moduleStr, dataStr, type));
                            }
                            
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