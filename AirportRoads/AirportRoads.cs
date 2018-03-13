using ICities;

using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Globalization;

using UnityEngine;

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace AirportRoads
{
    public class AirportRoads : LoadingExtensionBase, IUserMod
    {
        #region IUserMod implementation
        public string Name
        {
            get { return "Airport Roads " + version; }
        }

        public string Description
        {
            get { return "Adds airport runways and taxiways to the game."; }
        }
        #endregion

        public const string version = "1.3.5";

        public static AirportRoads instance;
        private GameObject m_gameObject;

        public override void OnLevelLoaded(LoadMode mode)
        {
            instance = this;
            if (mode == LoadMode.LoadAsset || mode == LoadMode.NewAsset)
            {
                if (m_gameObject == null || m_gameObject.name != "AirportRoads")
                {
                    m_gameObject = new GameObject("AirportRoads");
                    m_gameObject.AddComponent<AirportRoadsRoutine>();
                }
            }
            else if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                try
                {
                    panelGameObject = GameObject.Find("PublicTransportPlanePanel");

                    if (panelGameObject == null)
                    {
                        DebugUtils.Log("PublicTransportPlanePanel not found.");
                        return;
                    }

                    DebugUtils.Log(panelGameObject.name + " found.");

                    LoadResources();
                    InitMod();
                }
                catch (Exception e)
                {
                    DebugUtils.Log("Failed to load.");
                    Debug.LogException(e);
                }
            }
        }

        public override void OnLevelUnloading()
        {
            if (m_gameObject != null)
            {
                GameObject.Destroy(m_gameObject);
                m_gameObject = null;
            }
        }

        public static GameObject panelGameObject;

        private UITextureAtlas m_atlas;

        public void InitMod()
        {
            GeneratedScrollPanel panel = panelGameObject.GetComponent<GeneratedScrollPanel>();

            ShowNetwork("Airplane Runway", "Runway", panel, 7000, 600, "Runway");
            ShowNetwork("Airplane Taxiway", "Taxiway", panel, 4000, 200, "Taxiway");

            OptionPanelBase optionPanel = (OptionPanelBase)typeof(GeneratedScrollPanel).GetMethod("CreateOptionPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { "RoadsOptionPanel" });
            optionPanel.HidePanel();
            typeof(GeneratedScrollPanel).GetField("m_QuaysOptionPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(panel, optionPanel);
        }

        private void ShowNetwork(string name, string desc, GeneratedScrollPanel panel, int constructionCost, int maintenanceCost, string prefixIcon)
        {
            UIButton button = panel.Find<UIButton>(name);
            if (button != null && button.name == name) GameObject.DestroyImmediate(button);

            NetInfo netInfo = PrefabCollection<NetInfo>.FindLoaded(name);

            if (netInfo == null)
            {
                DebugUtils.Warning("Couldn't find NetInfo named '" + name + "'");
                return;
            }

            DebugUtils.Log("NetInfo named '" + name + "' found.");

            PlayerNetAI netAI = netInfo.m_netAI as PlayerNetAI;

            // Adding cost
            netAI.m_constructionCost = constructionCost;
            netAI.m_maintenanceCost = maintenanceCost;

            // Making the prefab valid
            netInfo.m_availableIn = ItemClass.Availability.All;
            netInfo.m_placementStyle = ItemClass.Placement.Manual;
            typeof(NetInfo).GetField("m_UICategory", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(netInfo, "PublicTransportPlane");

            // Changing the item class
            ItemClass itemClass = netInfo.m_class;
            netInfo.m_class = ScriptableObject.CreateInstance<ItemClass>();
            netInfo.m_class.m_layer = itemClass.m_layer;
            netInfo.m_class.m_level = itemClass.m_level;
            netInfo.m_class.m_service = ItemClass.Service.PublicTransport;
            netInfo.m_class.m_subService = ItemClass.SubService.PublicTransportPlane;

            // Adding icons
            netInfo.m_Atlas = m_atlas;
            netInfo.m_Thumbnail = prefixIcon;

            // Adding missing locale
            Locale locale = (Locale)typeof(LocaleManager).GetField("m_Locale", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SingletonLite<LocaleManager>.instance);
            Locale.Key key = new Locale.Key() { m_Identifier = "NET_TITLE", m_Key = name };
            if (!locale.Exists(key)) locale.AddLocalizedString(key, name);
            key = new Locale.Key() { m_Identifier = "NET_DESC", m_Key = name };
            if (!locale.Exists(key)) locale.AddLocalizedString(key, desc);

            typeof(GeneratedScrollPanel).GetMethod("CreateAssetItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { netInfo });
        }

        public void LoadResources()
        {
            // Add tooltip images into the atlas
            UITextureAtlas atlas = GetAtlas("TooltipSprites");
            if (atlas != null && atlas["Airplane Runway"] == null)
            {
                Texture2D[] textures = new Texture2D[]
                {
                    loadTextureFromAssembly("AirportRoads.Icons.RunwayTooltip.png"),
                    loadTextureFromAssembly("AirportRoads.Icons.TaxiwayTooltip.png")
                };
                textures[0].name = "Airplane Runway";
                textures[1].name = "Airplane Taxiway";

                AddTexturesInAtlas(atlas, textures);
            }

            // Create icon atlas
            if (m_atlas == null)
            {
                string[] spriteNames = new string[]
			    {
				    "Runway",
				    "RunwayDisabled",
				    "RunwayFocused",
				    "RunwayHovered",
				    "RunwayPressed",
				    "Taxiway",
				    "TaxiwayDisabled",
				    "TaxiwayFocused",
				    "TaxiwayHovered",
				    "TaxiwayPressed"
			    };
                m_atlas = CreateTextureAtlas("AirportRoads", spriteNames, "AirportRoads.Icons.");
            }
        }

        private UITextureAtlas CreateTextureAtlas(string atlasName, string[] spriteNames, string assemblyPath)
        {
            int maxSize = 1024;
            Texture2D texture2D = new Texture2D(maxSize, maxSize, TextureFormat.ARGB32, false);
            Texture2D[] textures = new Texture2D[spriteNames.Length];
            Rect[] regions = new Rect[spriteNames.Length];

            for (int i = 0; i < spriteNames.Length; i++)
                textures[i] = loadTextureFromAssembly(assemblyPath + spriteNames[i] + ".png");

            regions = texture2D.PackTextures(textures, 2, maxSize);

            UITextureAtlas textureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Material material = UnityEngine.Object.Instantiate<Material>(UIView.GetAView().defaultAtlas.material);
            material.mainTexture = texture2D;
            textureAtlas.material = material;
            textureAtlas.name = atlasName;

            for (int i = 0; i < spriteNames.Length; i++)
            {
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo
                {
                    name = spriteNames[i],
                    texture = textures[i],
                    region = regions[i],
                };

                textureAtlas.AddSprite(item);
            }

            return textureAtlas;
        }

        private Texture2D loadTextureFromAssembly(string path)
        {
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);

            byte[] array = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(array, 0, array.Length);

            Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            texture2D.LoadImage(array);

            return texture2D;
        }

        private void AddTexturesInAtlas(UITextureAtlas atlas, Texture2D[] newTextures)
        {
            Texture2D[] textures = new Texture2D[atlas.count + newTextures.Length];

            for (int i = 0; i < atlas.count; i++)
            {
                // Locked textures workaround
                Texture2D texture2D = atlas.sprites[i].texture;

                if (texture2D != null)
                {
                    RenderTexture renderTexture = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0);
                    Graphics.Blit(texture2D, renderTexture);

                    RenderTexture active = RenderTexture.active;
                    texture2D = new Texture2D(renderTexture.width, renderTexture.height);
                    RenderTexture.active = renderTexture;
                    texture2D.ReadPixels(new Rect(0f, 0f, (float)renderTexture.width, (float)renderTexture.height), 0, 0);
                    texture2D.Apply();
                    RenderTexture.active = active;

                    RenderTexture.ReleaseTemporary(renderTexture);

                    textures[i] = texture2D;
                    textures[i].name = atlas.sprites[i].name;
                }
            }

            for (int i = 0; i < newTextures.Length; i++)
                textures[atlas.count + i] = newTextures[i];

            Rect[] regions = atlas.texture.PackTextures(textures, atlas.padding, 4096, false);

            atlas.sprites.Clear();

            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] != null)
                {
                    UITextureAtlas.SpriteInfo spriteInfo = atlas[textures[i].name];
                    atlas.sprites.Add(new UITextureAtlas.SpriteInfo
                    {
                        texture = textures[i],
                        name = textures[i].name,
                        border = (spriteInfo != null) ? spriteInfo.border : new RectOffset(),
                        region = regions[i]
                    });
                }
            }

            atlas.RebuildIndexes();
        }

        public static UITextureAtlas GetAtlas(string name)
        {
            UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i].name == name)
                    return atlases[i];
            }

            return UIView.GetAView().defaultAtlas;
        }
    }

    public class AirportRoadsRoutine : MonoBehaviour
    {
        public void Update()
        {
            if (AirportRoads.panelGameObject != null) return;

            try
            {
                AirportRoads.panelGameObject = GameObject.Find("LandscapingPathsPanel");

                if (AirportRoads.panelGameObject == null) return;

                DebugUtils.Log(AirportRoads.panelGameObject.name + " found.");

                AirportRoads.instance.LoadResources();
                AirportRoads.instance.InitMod();
            }
            catch (Exception e)
            {
                DebugUtils.Log("Failed to load.");
                Debug.LogException(e);
            }
        }
    }
}