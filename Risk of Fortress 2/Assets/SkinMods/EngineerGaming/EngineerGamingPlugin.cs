using BepInEx;
using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Security;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;

#pragma warning disable CS0618 // Type or member is obsolete
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: R2API.Utils.ManualNetworkRegistration]
[assembly: EnigmaticThunder.Util.ManualNetworkRegistration]
namespace EngineerGaming
{
    
    [BepInPlugin("com.Iceflame.EngineerGaming","EngineerGaming","1.0.0")]
    public partial class EngineerGamingPlugin : BaseUnityPlugin
    {
        internal static EngineerGamingPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger => Instance?.Logger;
        
        private static AssetBundle assetBundle;
        private static readonly List<Material> materialsWithRoRShader = new List<Material>();
        private void Awake()
        {
            Instance = this;
            BeforeAwake();
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EngineerGaming.iceflameengineergaming"))
            {
                assetBundle = AssetBundle.LoadFromStream(assetStream);
            }

            BodyCatalog.availability.CallWhenAvailable(BodyCatalogInit);
            HookEndpointManager.Add(typeof(Language).GetMethod(nameof(Language.LoadStrings)), (Action<Action<Language>, Language>)LanguageLoadStrings);

            ReplaceShaders();

            AfterAwake();
        }

        partial void BeforeAwake();
        partial void AfterAwake();
        static partial void BeforeBodyCatalogInit();
        static partial void AfterBodyCatalogInit();

        private static void ReplaceShaders()
        {
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Resources/Engi/FullEngiMat.mat", @"Hopoo Games/Deferred/Standard"));
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Resources/Scout/ScoutMat.mat", @"Hopoo Games/Deferred/Standard"));
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Resources/Soldier/Bot_Mat.mat", @"Hopoo Games/Deferred/Standard"));
            materialsWithRoRShader.Add(LoadMaterialWithReplacedShader(@"Assets/Resources/Soldier/SoldierMat.mat", @"Hopoo Games/Deferred/Standard"));
        }

        private static Material LoadMaterialWithReplacedShader(string materialPath, string shaderName)
        {
            var material = assetBundle.LoadAsset<Material>(materialPath);
            material.shader = Shader.Find(shaderName);

            return material;
        }

        private static void LanguageLoadStrings(Action<Language> orig, Language self)
        {
            orig(self);

            self.SetStringByToken("ICEFLAME_SKIN_ENGINEERGAMINGSKIN_NAME", "EngineerGaming");
            self.SetStringByToken("ICEFLAME_SKIN_MERCSCOUT_NAME", "BONK");
            self.SetStringByToken("ICEFLAME_SKIN_LOADERSOLDIER_NAME", "Soldier");

        }

        private static void Nothing(Action<SkinDef> orig, SkinDef self)
        {

        }

        private static void BodyCatalogInit()
        {
            BeforeBodyCatalogInit();

            var awake = typeof(SkinDef).GetMethod(nameof(SkinDef.Awake), BindingFlags.NonPublic | BindingFlags.Instance);
            HookEndpointManager.Add(awake, (Action<Action<SkinDef>, SkinDef>)Nothing);

            AddEngiBodyEngineerGamingSkinSkin();
            AddMercBodyMercScoutSkin();
            AddLoaderBodyLoaderSoldierSkin();
            
            HookEndpointManager.Remove(awake, (Action<Action<SkinDef>, SkinDef>)Nothing);

            AfterBodyCatalogInit();
        }

        static partial void EngiBodyEngineerGamingSkinSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddEngiBodyEngineerGamingSkinSkin()
        {
            var bodyName = "EngiBody";
            var skinName = "EngineerGamingSkin";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl.GetComponent<ModelSkinController>();

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Engi/EngiIcon.jpg");
                skin.name = skinName;
                skin.nameToken = "ICEFLAME_SKIN_ENGINEERGAMINGSKIN_NAME";
                skin.rootObject = mdl;
                skin.baseSkins = new SkinDef[] 
                { 
                    skinController.skins[0],
                };
                skin.unlockableDef = null;
                skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                skin.rendererInfos = new CharacterModel.RendererInfo[]
                {
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Engi/FullEngiMat.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = renderers[4]
                    },
                };
                skin.meshReplacements = new SkinDef.MeshReplacement[]
                {
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\EngineerGaming\Meshes\Engineer.mesh"),
                        renderer = renderers[4]
                    },
                };
                skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                EngiBodyEngineerGamingSkinSkinAdded(skin, bodyPrefab);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void MercBodyMercScoutSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddMercBodyMercScoutSkin()
        {
            var bodyName = "MercBody";
            var skinName = "MercScout";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl.GetComponent<ModelSkinController>();

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                skin.icon = assetBundle.LoadAsset<Sprite>(@"Assets/Resources/Scout/Icon_scout.jpg");
                skin.name = skinName;
                skin.nameToken = "ICEFLAME_SKIN_MERCSCOUT_NAME";
                skin.rootObject = mdl;
                skin.baseSkins = new SkinDef[] 
                { 
                    skinController.skins[0],
                };
                skin.unlockableDef = null;
                skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                skin.rendererInfos = new CharacterModel.RendererInfo[]
                {
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Scout/ScoutMat.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = renderers[3]
                    },
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Scout/ScoutMat.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                        ignoreOverlays = false,
                        renderer = renderers[4]
                    },
                };
                skin.meshReplacements = new SkinDef.MeshReplacement[]
                {
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\EngineerGaming\Meshes\Scout.mesh"),
                        renderer = renderers[3]
                    },
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\EngineerGaming\Meshes\ScoutBat.mesh"),
                        renderer = renderers[4]
                    },
                };
                skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                MercBodyMercScoutSkinAdded(skin, bodyPrefab);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }

        static partial void LoaderBodyLoaderSoldierSkinAdded(SkinDef skinDef, GameObject bodyPrefab);

        private static void AddLoaderBodyLoaderSoldierSkin()
        {
            var bodyName = "LoaderBody";
            var skinName = "LoaderSoldier";
            try
            {
                var bodyPrefab = BodyCatalog.FindBodyPrefab(bodyName);
                var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
                var mdl = modelLocator.modelTransform.gameObject;
                var skinController = mdl.GetComponent<ModelSkinController>();

                var renderers = mdl.GetComponentsInChildren<Renderer>(true);

                var skin = ScriptableObject.CreateInstance<SkinDef>();
                skin.icon = null;
                skin.name = skinName;
                skin.nameToken = "ICEFLAME_SKIN_LOADERSOLDIER_NAME";
                skin.rootObject = mdl;
                skin.baseSkins = new SkinDef[] 
                { 
                    skinController.skins[0],
                };
                skin.unlockableDef = null;
                skin.gameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>();
                skin.rendererInfos = new CharacterModel.RendererInfo[]
                {
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Soldier/Bot_Mat.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                        ignoreOverlays = false,
                        renderer = renderers[2]
                    },
                    new CharacterModel.RendererInfo
                    {
                        defaultMaterial = assetBundle.LoadAsset<Material>(@"Assets/Resources/Soldier/SoldierMat.mat"),
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                        ignoreOverlays = false,
                        renderer = renderers[0]
                    },
                };
                skin.meshReplacements = new SkinDef.MeshReplacement[]
                {
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\EngineerGaming\Meshes\MechSoldier.mesh"),
                        renderer = renderers[2]
                    },
                    new SkinDef.MeshReplacement
                    {
                        mesh = assetBundle.LoadAsset<Mesh>(@"Assets\SkinMods\EngineerGaming\Meshes\Soldier.mesh"),
                        renderer = renderers[0]
                    },
                };
                skin.minionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>();
                skin.projectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>();

                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[skinController.skins.Length - 1] = skin;

                BodyCatalog.skins[(int)BodyCatalog.FindBodyIndex(bodyPrefab)] = skinController.skins;
                LoaderBodyLoaderSoldierSkinAdded(skin, bodyPrefab);
            }
            catch (Exception e)
            {
                InstanceLogger.LogWarning($"Failed to add \"{skinName}\" skin to \"{bodyName}\"");
                InstanceLogger.LogError(e);
            }
        }
    }

}

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}

namespace EnigmaticThunder.Util
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute { }
}
