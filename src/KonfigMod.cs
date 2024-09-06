using SpaceWarp.API.Mods;
using SpaceWarp;
using BepInEx;
using ReLIB;
using ReLIB.UI;
using Logger = ReLIB.logging.Logger;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using KSP.Modules;
using KSP.Game;
using KSP.Sim.Definitions;
using KSP.Messages;
using KSP.UI.Binding;
using KSP.Sim.impl;
using KSP.Sim;
using KSP.Sim.ResourceSystem;
using HarmonyLib;
using KSP.FX.LaunchSystems;
using KSP.Rendering.impl;
using KSP.Rendering;
using System.Collections;

namespace Konfig;
public abstract class PatchModule<M, D>
{

    public abstract void Patch(ref M Module, ref D Data, string partName, PartData partData, PartCore Target);


}
public class PatchListData
{
    public string ModuleName { get; set; }
    public string DataName { get; set; }
    public Type PatchType { get; set; }
    public dynamic originalData { get; set; } = null;
    public string FileLocation { get; set; }
    public PatchListData(string mn, string dn, Type pt, string fileLocation)
    {
        ModuleName = mn;
        DataName = dn;
        PatchType = pt;
        FileLocation = fileLocation;
    }
    public void OriginalData<a>(a Odata)
    {
        originalData = Odata;
    }
}
public static class KonfigHpatch
{
    public static bool CustomSkybox = false;
    public static Cubemap SkyboxMap { get; set; } = null;
    public static void SetCM()
    {

        //ShaderHelper.Shaders.SkyboxCubemap.
        //Shader.SetGlobalTexture(ShaderHelper._GalaxyCubemapTexture, KonfigHpatch.SkyboxMap);
        //Shader.SetGlobalTexture(ShaderHelper._ObserverCubemapTexture, KonfigHpatch.SkyboxMap);
        Shader.SetGlobalTexture("_MainTex_HDR", KonfigHpatch.SkyboxMap);
        Shader.SetGlobalTexture("RK_GALAXY_CUBEMAP", KonfigHpatch.SkyboxMap);
        //Shader.SetGlobalTexture(ShaderHelper.RK_OBSERVER_CUBEMAP, KonfigHpatch.SkyboxMap);
        Shader.SetGlobalTexture("_MainTex", KonfigHpatch.SkyboxMap);
        Shader.SetGlobalTexture("_galaxySkybox", KonfigHpatch.SkyboxMap);
        //Shader.SetGlobalTexture("_GalaxyCubemapTexture", KonfigHpatch.SkyboxMap);
        Shader.SetGlobalTexture("_oabSkybox", KonfigHpatch.SkyboxMap);
        Shader.SetGlobalTexture("_spaceTex", KonfigHpatch.SkyboxMap);
        RenderSettings.skybox.SetTexture("_galaxySkybox", KonfigHpatch.SkyboxMap);
        RenderSettings.skybox.SetTexture("_oabSkybox", KonfigHpatch.SkyboxMap);
        RenderSettings.skybox.SetTexture("_MainTex", KonfigHpatch.SkyboxMap);
        RenderSettings.skybox.SetTexture("_MainTex_HDR", KonfigHpatch.SkyboxMap);
    }

    [HarmonyPatch(typeof(CubemapReflectionSystem))]
    [HarmonyPatch("OnOABUnloaded")]
    [HarmonyPostfix]
    public static void CubemapReflectionSystem_OnOABUnloaded(CubemapReflectionSystem __instance)
    {
        if (CustomSkybox)
        {
            SetCM();
        }
    }
    [HarmonyPatch(typeof(CubemapReflectionSystem))]
    [HarmonyPatch("OnOABLoaded")]
    [HarmonyPostfix]
    public static void CubemapReflectionSystem_OnOABLoaded(CubemapReflectionSystem __instance)
    {
        if (CustomSkybox)
        {
            SetCM();
        }
    }
    [HarmonyPatch(typeof(CubemapReflectionSystem))]
    [HarmonyPatch("OnMapExited")]
    [HarmonyPostfix]
    public static void CubemapReflectionSystem_OnMapExited(CubemapReflectionSystem __instance)
    {
        if (CustomSkybox)
        {
            SetCM();
        }
    }
    [HarmonyPatch(typeof(CubemapReflectionSystem))]
    [HarmonyPatch("OnMapEntered")]
    [HarmonyPostfix]
    public static void CubemapReflectionSystem_OnMapEntered(CubemapReflectionSystem __instance)
    {
        if (CustomSkybox)
        {
            SetCM();
        }
    }
    [HarmonyPatch(typeof(ProceduralCubemap))]
    [HarmonyPatch("CreateCubeMap")]
    [HarmonyPrefix]
    public static bool ProceduralCubemap_CreateCubeMap(ProceduralCubemap __instance, ref Cubemap __result)
    {
        if (CustomSkybox)
        {
            __result = SkyboxMap;
            //SetCM();
        }
        return !CustomSkybox;
    }
    [HarmonyPatch(typeof(ProceduralCubemap))]
    [HarmonyPatch("RenderCubemap")]
    [HarmonyPrefix]
    public static bool ProceduralCubemap_RenderCubemap(ProceduralCubemap __instance, ref Cubemap __result)
    {
        if (CustomSkybox)
        {
            __result = SkyboxMap;
            //SetCM();
        }
        return !CustomSkybox;
    }

    [HarmonyPatch(typeof(ObserverCubemapView))]
    [HarmonyPatch("UpdateCubemap")]
    [HarmonyPrefix]
    public static bool ObserverCubemapView_UpdateCubemap(ObserverCubemapView __instance)
    {
        if (CustomSkybox)
        {
            SetCM();
        }
        return !CustomSkybox;
    }
    [HarmonyPatch(typeof(ApplyCubemaps))]
    [HarmonyPatch("UpdateCubemaps")]
    [HarmonyPrefix]
    public static bool ApplyCubemaps_UpdateCubemaps(ApplyCubemaps __instance)
    {
        if (CustomSkybox)
        {
            SetCM();
        }
        return !CustomSkybox;
    }
}

public class SkyBoxUtils
{
    public string BaseLocation { get; set; }
    public string TargetFile { get; set; }
    public int size { get; set; }
    public void Create()
    {
        CreateSkybox(TargetFile, size);
    }
    public void CreateSkybox(string TexName, int Tsize)
    {
        Cubemap cubemap = new Cubemap(Tsize, TextureFormat.RGB24, 1);
        Texture2D tXP = new Texture2D(Tsize, Tsize);
        Texture2D tXN = new Texture2D(Tsize, Tsize);
        Texture2D tYP = new Texture2D(Tsize, Tsize);
        Texture2D tYN = new Texture2D(Tsize, Tsize);
        Texture2D tZP = new Texture2D(Tsize, Tsize);
        Texture2D tZN = new Texture2D(Tsize, Tsize);
        tXP.LoadImage(File.ReadAllBytes($"{BaseLocation}/{TexName}.xp.png"));
        tXN.LoadImage(File.ReadAllBytes($"{BaseLocation}/{TexName}.xn.png"));
        tYP.LoadImage(File.ReadAllBytes($"{BaseLocation}/{TexName}.yp.png"));
        tYN.LoadImage(File.ReadAllBytes($"{BaseLocation}/{TexName}.yn.png"));
        tZP.LoadImage(File.ReadAllBytes($"{BaseLocation}/{TexName}.zp.png"));
        tZN.LoadImage(File.ReadAllBytes($"{BaseLocation}/{TexName}.zn.png"));

        void SetCubeMapFace(CubemapFace face, Color[] CubeMapColors)
        {
            cubemap.SetPixels(CubeMapColors, face);
        }
        SetCubeMapFace(CubemapFace.PositiveX, tXP.GetPixels());
        SetCubeMapFace(CubemapFace.NegativeX, tXN.GetPixels());
        SetCubeMapFace(CubemapFace.PositiveY, tYP.GetPixels());
        SetCubeMapFace(CubemapFace.NegativeY, tYN.GetPixels());
        SetCubeMapFace(CubemapFace.PositiveZ, tZP.GetPixels());
        SetCubeMapFace(CubemapFace.NegativeZ, tZN.GetPixels());
        cubemap.Apply();
        KonfigHpatch.CustomSkybox = true;
        KonfigHpatch.SkyboxMap = cubemap;
        KonfigHpatch.SetCM();
    }
}

[BepInPlugin("com.rendevouzrs_entertainment.konfig", "Konfig", "0.1.2")]
[BepInDependency(ReLIBMod.ModId, ReLIBMod.ModVersion)]

public class KonfigMod : BaseUnityPlugin
{
    public static string ModId = "com.rendevouzrs_entertainment.Konfig";
    public static string ModName = "Konfig";
    public static string ModVersion = "0.1.2";

    private static string LocationFile = Assembly.GetExecutingAssembly().Location;
    private static string LocationDirectory = Path.GetDirectoryName(LocationFile);

    public bool patchRan = false;
    public Dictionary<string, List<PatchListData>> PatchList = new Dictionary<string, List<PatchListData>>();

    private Logger logger = new Logger(ModName, ModVersion);
    public static Manager manager;




    public static bool IsDev = false;
    public IEnumerator Init()
    {
        yield return new WaitForSeconds(10);
        bool notInitilised = true;
        while (notInitilised)
        {
            try
            {
                GameManager.Instance.Game.Messages.Subscribe<GameStateChangedMessage>(GameStateChanged);
                notInitilised = false;
                GetConfigs();
                Harmony.CreateAndPatchAll(typeof(KonfigHpatch));
                logger.Log("Initialized");
            }
            catch (Exception e)
            {

            }
            yield return new WaitForSeconds(1);
        }

    }
    void Awake()
    {

        if (IsDev)
        {
            ReLIBMod.EnableDebugMode();
        }


    }
    void Start()
    {
        ReLIBMod.RunCr(Init());
    }
    void GameStateChanged(MessageCenterMessage messageCenterMessage)
    {
        GameStateChangedMessage gameStateChangedMessage = messageCenterMessage as GameStateChangedMessage;
        logger.Debug($"{gameStateChangedMessage.CurrentState}");
        if (gameStateChangedMessage.CurrentState == GameState.Loading || gameStateChangedMessage.CurrentState == GameState.FlightView)
        {
            RunPatchers();
            patchRan = true;
        }

    }
    void RunPatchers()
    {
        try
        {

            foreach (string partPatches in PatchList.Keys)
            {

                foreach (PatchListData PatchType in PatchList[partPatches])
                {
                    try
                    {//KSP.Rendering.impl.ObserverCubemapView
                        if (PatchType.ModuleName == "SkyBoxUtils")
                        {
                            SkyBoxUtils skyBoxUtils = new SkyBoxUtils();
                            int skyboxSize = 2048;
                            object obj = Activator.CreateInstance(PatchType.PatchType);
                            var m = PatchType.PatchType.GetMethod("Patch");
                            m.Invoke(obj, new object[] { skyBoxUtils, skyboxSize, partPatches, null, null });

                        }
                        if (PatchType.ModuleName == "ResourceDefinition")
                        {
                            var resourceID = GameManager.Instance.Game.ResourceDefinitionDatabase.GetResourceIDFromName(partPatches);
                            var resourceDefData = GameManager.Instance.Game.ResourceDefinitionDatabase.GetDefinitionData(resourceID);

                            ResourceDefinition rd = new ResourceDefinition()
                            {
                                abbreviationKey = resourceDefData.abbreviationKey,
                                costPerUnit = resourceDefData.resourceProperties.costPerUnit,
                                displayNameKey = resourceDefData.displayNameKey,
                                flowMode = resourceDefData.resourceProperties.flowMode,
                                ignoreForIsp = resourceDefData.resourceProperties.ignoreForIsp,
                                isTweakable = resourceDefData.resourceProperties.isTweakable,
                                isVisible = resourceDefData.resourceProperties.isVisible,
                                massPerUnit = resourceDefData.resourceProperties.massPerUnit,
                                name = resourceDefData.name,
                                NonStageable = resourceDefData.resourceProperties.NonStageable,
                                resourceIconAssetAddress = resourceDefData.resourceIconAssetAddress,
                                specificHeatCapacityPerUnit = resourceDefData.resourceProperties.specificHeatCapacityPerUnit,
                                transferMode = resourceDefData.resourceProperties.transferMode,
                                vfxFuelType = resourceDefData.vfxFuelType,
                                volumePerUnit = resourceDefData.resourceProperties.volumePerUnit
                            };
                            try
                            {
                                if (PatchType.originalData == null)
                                {
                                    PatchType.OriginalData<ResourceDefinition>(rd);
                                }
                            }
                            catch (Exception e)
                            {

                            }
                            rd = (ResourceDefinition)PatchType.originalData;
                            object obj = Activator.CreateInstance(PatchType.PatchType);
                            var m = PatchType.PatchType.GetMethod("Patch");
                            m.Invoke(obj, new object[] { rd, null, partPatches, null, null });
                        }
                        if (PatchType.ModuleName == "CelestialBodyComponent")
                        {
                            CelestialBodyComponent b = GameManager.Instance.Game.UniverseModel.FindCelestialBodyByName(partPatches);

                            CelestialBodyProperties cb = (CelestialBodyProperties)((CelestialBodyDefinition)b.GetDefinition()).properties;
                            try
                            {
                                if (PatchType.originalData == null)
                                {
                                    PatchType.OriginalData<CelestialBodyProperties>(cb);
                                }
                            }
                            catch (Exception e)
                            {

                            }
                            cb = (CelestialBodyProperties)PatchType.originalData;
                            object obj = Activator.CreateInstance(PatchType.PatchType);
                            var m = PatchType.PatchType.GetMethod("Patch");
                            m.Invoke(obj, new object[] { b, cb, partPatches, null, null });
                        }
                        if (PatchType.ModuleName.Contains("Module_"))
                        {
                            PartCore SelectedPartToPatch = GameManager.Instance.Game.Parts.Get(partPatches);
                            var partModule = SelectedPartToPatch.data.serializedPartModules.Find(partModule => partModule.Name == $"PartComponent{PatchType.ModuleName}");
                            var partData = partModule.ModuleData.Find(partData => partData.Name == PatchType.DataName);
                            try
                            {
                                if (PatchType.originalData == null)
                                {
                                    PatchType.OriginalData<SerializedModuleData>(partData);
                                }
                            }
                            catch (Exception e)
                            {

                            }
                            partData = PatchType.originalData;
                            object obj = Activator.CreateInstance(PatchType.PatchType);
                            var m = PatchType.PatchType.GetMethod("Patch");
                            m.Invoke(obj, new object[] { null, partData.DataObject, partPatches, SelectedPartToPatch.data, SelectedPartToPatch });
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
        }
    }


    void BuildPatch(string PatchPath, List<MetadataReference> references, string modules, int patchID)
    {
        //Match
        Regex regex = new Regex(@"^\[Target[(]\w+[)]]\n^\[Module[(]\w+[)]]\n^\[Data[(]\w+[)]]\n", RegexOptions.Multiline);

        string targetStr = "";
        string moduleStr = "";
        string dataStr = "";
        string patchData = "";


        string PatchFileData = String.Join("\n", File.ReadAllLines(Path.GetFullPath($@"{PatchPath}")));
        string[] Patches = regex.Split(PatchFileData);
        MatchCollection Patches_Headers = regex.Matches(PatchFileData);
        Patches = Patches.Where(w => w != "").ToArray();
        foreach (var patchd in Patches)
        {

            var patchHeader = Patches_Headers[patchID].Value;
            targetStr = Regex.Match(patchHeader, @"[(](.*?)[)]", RegexOptions.Multiline).Groups[0].Value;
            moduleStr = Regex.Match(patchHeader, @"[(](.*?)[)]", RegexOptions.Multiline).NextMatch().Groups[0].Value;
            dataStr = Regex.Match(patchHeader, @"[(](.*?)[)]", RegexOptions.Multiline).NextMatch().NextMatch().Groups[0].Value;
            targetStr = targetStr.Replace("(", "");
            targetStr = targetStr.Replace(")", "");
            moduleStr = moduleStr.Replace("(", "");
            moduleStr = moduleStr.Replace(")", "");
            dataStr = dataStr.Replace("(", "");
            dataStr = dataStr.Replace(")", "");
            patchData = patchd;

            // Predefined patch types
            if (moduleStr == "ResourceDefinition")
            {
                patchData = $$"""
{{patchData}}
GameManager.Instance.Game.ResourceDefinitionDatabase.AddOrUpdateDefinition(Module);
""";
            }
            if (moduleStr == "Skybox")
            {
                moduleStr = "SkyBoxUtils";
                patchData = $$"""
Module.BaseLocation = @"{{PatchPath}}";
Module.TargetFile = @"{{targetStr}}";
{{patchData}}
Module.size = Data;
Module.Create();
""";
            }

            string Patch = $$"""
{{modules}}

namespace KPatcher;

public class patch_{{targetStr}}_{{PatchPath.Split('\\')[^1]}}_{{patchID}} : PatchModule<{{moduleStr}},{{dataStr}}> {
    private Logger logger = new Logger("{{targetStr}} {{moduleStr}} {{dataStr}} {{patchID}} Patch", "");
    static void Main()
    {

    }
    public override void Patch(ref {{moduleStr}} Module, ref {{dataStr}} Data, string  partName,PartData partData, PartCore Target){
        {{patchData}}
    }
}
""";
            logger.Debug(Patch);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(Patch);

            CSharpCompilation compilation = CSharpCompilation.Create($"Patch_{targetStr}_{PatchPath.Split('\\')[^1]}_PatchModule_{patchID}", new[] { syntaxTree }, references);
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
                Type type = assembly.GetType($"KPatcher.patch_{targetStr}_{PatchPath.Split('\\')[^1]}_{patchID}");
                if (PatchList.ContainsKey(targetStr))
                {
                    PatchList[targetStr].Add(new PatchListData(moduleStr, dataStr, type, PatchPath));
                }
                else
                {
                    PatchList.Add(targetStr, new List<PatchListData>());
                    PatchList[targetStr].Add(new PatchListData(moduleStr, dataStr, type, PatchPath));
                }

            }
        }



    }

    void LoadALLPatches()
    {
        string modules = """
            using Konfig;
            using SpaceWarp.API.Mods;
            using SpaceWarp;
            using BepInEx;
            using ReLIB;
            using ReLIB.UI;
            using Logger = ReLIB.logging.Logger;
            using System.Text.RegularExpressions;
            using UnityEngine;
            using Newtonsoft.Json;
            using KSP.Modules;
            using KSP.Game;
            using KSP.Sim.Definitions;
            using KSP.Messages;
            using KSP.UI.Binding;
            using KSP.Sim.impl;
            using KSP.Sim;
            using KSP.Sim.ResourceSystem;
            """;

        int patchID = 0;
        //Clear previous patches
        PatchList.Clear();

        //Generate References
        List<MetadataReference> references = new List<MetadataReference>();
        foreach (var assembalyData in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                logger.Debug($"{assembalyData.Location} {assembalyData.FullName}");
                if (assembalyData.Location == null || assembalyData.Location == "" || assembalyData.Location == " ")
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
        references.Add(MetadataReference.CreateFromFile("./KSP2_x64_Data/Managed/Assembly-CSharp.dll"));
        try
        {
            references.ForEach(x =>
            {
                logger.Debug($"{x.Display}");

            });
        }
        catch (Exception e)
        {
            logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
        }


        //Load Patch data
        logger.Log("Getting patches");
        foreach (string dir in Directory.EnumerateDirectories(Path.GetFullPath($@"{LocationDirectory}\..\")))
        {
            logger.Log($"Searching {dir}");
            foreach (string konfigLocation in Directory.EnumerateFiles(Path.GetFullPath($@"{dir}"), "*.konfig"))
            {
                logger.Log($"Found patch file {konfigLocation}");
                BuildPatch(Path.GetFullPath($@"{konfigLocation}"), references, modules, patchID);



                patchID++;
            }
        }
    }
    //OLD
    void GetConfigs()
    {
        Regex regex = new Regex(@"^\[Target[(]\w+[)]]\n^\[Module[(]\w+[)]]\n^\[Data[(]\w+[)]]\n", RegexOptions.Multiline);
        PatchList.Clear();
        try
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (var assembalyData in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    logger.Debug($"{assembalyData.Location} {assembalyData.FullName}");
                    if (assembalyData.Location == null || assembalyData.Location == "" || assembalyData.Location == " ")
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
            references.Add(MetadataReference.CreateFromFile("./KSP2_x64_Data/Managed/Assembly-CSharp.dll"));
            try
            {
                references.ForEach(x =>
                {
                    logger.Debug($"{x.Display}");

                });
            }
            catch (Exception e)
            {
                logger.Error($"{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}");
            }

            logger.Log("Getting patches");
            foreach (string dir in Directory.EnumerateDirectories(Path.GetFullPath($@"{LocationDirectory}\..\")))
            {
                logger.Log($"Searching {dir}");
                foreach (string konfigLoc in Directory.EnumerateFiles(Path.GetFullPath($@"{dir}"), "*.konfig"))
                {
                    logger.Log($"Found patch file {konfigLoc}");
                    int patchID = 0;
                    string PatchData = String.Join("\n", File.ReadAllLines(Path.GetFullPath($@"{konfigLoc}")));
                    string[] Patches = regex.Split(PatchData);
                    MatchCollection Patches_Headers = regex.Matches(PatchData);
                    logger.Debug(PatchData);
                    Patches = Patches.Where(w => w != "").ToArray();
                    foreach (var patchd in Patches)
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
                        string patch = patchd;
                        logger.Debug(patch);
                        if (moduleStr == "ResourceDefinition")
                        {
                            patch = $$"""
{{patch}}
GameManager.Instance.Game.ResourceDefinitionDatabase.AddOrUpdateDefinition(Module);
""";
                        }
                        if (moduleStr == "Skybox")
                        {
                            moduleStr = "SkyBoxUtils";
                            patch = $$"""
Module.BaseLocation = @"{{dir}}";
Module.TargetFile = @"{{targetStr}}";
{{patch}}
Module.size = Data;
Module.Create();
""";
                        }

                        logger.Debug($$"""
using Konfig;
using SpaceWarp.API.Mods;
using SpaceWarp;
using BepInEx;
using ReLIB;
using ReLIB.UI;
using Logger = ReLIB.logging.Logger;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;
using KSP.Modules;
using KSP.Game;
using KSP.Sim.Definitions;
using KSP.Messages;
using KSP.UI.Binding;
using KSP.Sim.impl;
using KSP.Sim;
using KSP.Sim.ResourceSystem;

namespace KPatcher;

public class patch_{{targetStr}}_{{dir.Split('\\')[dir.Split('\\').Length - 1]}}_{{patchID}} : PatchModule<{{moduleStr}},{{dataStr}}> {
    private Logger logger = new Logger("Konfig Patch", "0.1.2");
    static void Main()
    {

    }
    public override void Patch(ref {{moduleStr}} Module, ref {{dataStr}} Data, string  partName,PartData partData, PartCore Target){
        {{patch}}
    }
}
""");
                        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText($$"""
using Konfig;
using SpaceWarp.API.Mods;
using SpaceWarp;
using BepInEx;
using ReLIB;
using ReLIB.UI;
using Logger = ReLIB.logging.Logger;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;
using KSP.Modules;
using KSP.Game;
using KSP.Sim.Definitions;
using KSP.Messages;
using KSP.UI.Binding;
using KSP.Sim.impl;
using KSP.Sim;
using KSP.Sim.ResourceSystem;

namespace KPatcher;

public class patch_{{targetStr}}_{{dir.Split('\\')[dir.Split('\\').Length - 1]}}_{{patchID}} : PatchModule<{{moduleStr}},{{dataStr}}> {
    private Logger logger = new Logger("Konfig Patch", "0.1.2");
    static void Main()
    {

    }
    public override void Patch(ref {{moduleStr}} Module, ref {{dataStr}} Data, string  partName,PartData partData, PartCore Target){
        {{patch}}
    }
}
""");


                        CSharpCompilation compilation = CSharpCompilation.Create($"Patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]}_PatchModule_{patchID}", new[] { syntaxTree }, references);
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
                            Type type = assembly.GetType($"KPatcher.patch_{targetStr}_{dir.Split('\\')[dir.Split('\\').Length - 1]}_{patchID}");

                            //var x = GameManager.Instance.Game.Parts.Get("");
                            //x.data
                            if (PatchList.ContainsKey(targetStr))
                            {
                                PatchList[targetStr].Add(new PatchListData(moduleStr, dataStr, type, dir));
                            }
                            else
                            {
                                PatchList.Add(targetStr, new List<PatchListData>());
                                PatchList[targetStr].Add(new PatchListData(moduleStr, dataStr, type, dir));
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