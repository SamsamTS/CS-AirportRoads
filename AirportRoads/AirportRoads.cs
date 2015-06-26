using ICities;

using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Globalization;

using UnityEngine;

using System;
using System.IO;
using System.Reflection;


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

        public const string version = "1.2";

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                try
                {
                    LoadResources();
                    InitMod();
                }
                catch(Exception e)
                {
                    DebugUtils.Warning("Failed to load.");
                    Debug.LogException(e);
                }
            }
        }

        private UITextureAtlas m_atlas;

        private void InitMod()
        {
            PublicTransportPanel panel = GameObject.Find("PublicTransportPlanePanel").GetComponent<PublicTransportPanel>();
            ShowNetwork("Airplane Runway", "Runway", panel, "runway_", 7000, 600);
            ShowNetwork("Airplane Taxiway", "Taxiway", panel, "taxiway_", 4000, 200);
        }

        private void ShowNetwork(string name, string desc, GeneratedScrollPanel panel, string prefixIcon, int constructionCost, int maintenanceCost)
        {
            if (panel.Find<UIButton>(name) != null) return;

            var netInfo = PrefabCollection<NetInfo>.FindLoaded(name);

            if (netInfo == null)
            {
                DebugUtils.Warning("Couldn't find NetInfo named '" + name + "'");
                return;
            }

            DebugUtils.Log("NetInfo named '" + name + "' found.");

            PlayerNetAI netAI = netInfo.m_netAI as PlayerNetAI;
            netAI.m_constructionCost = constructionCost;
            netAI.m_maintenanceCost = maintenanceCost;

            Locale locale = (Locale)typeof(LocaleManager).GetField("m_Locale", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SingletonLite<LocaleManager>.instance);
            Locale.Key key = new Locale.Key() { m_Identifier = "NET_TITLE", m_Key = name };
            if(!locale.Exists(key)) locale.AddLocalizedString(key, name);
            key = new Locale.Key() { m_Identifier = "NET_DESC", m_Key = name };
            if(!locale.Exists(key)) locale.AddLocalizedString(key, desc);

            typeof(GeneratedScrollPanel).GetMethod("CreateAssetItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { netInfo });
            
            UIButton button = panel.Find<UIButton>(name);
            button.atlas = m_atlas;
            button.size = new Vector2(109, 75);
            button.normalBgSprite = prefixIcon + "default";
            button.disabledBgSprite = prefixIcon + "disabled";
            button.hoveredBgSprite = prefixIcon + "hover";
            button.pressedBgSprite = prefixIcon + "pressed";
            button.focusedBgSprite = prefixIcon + "selected";
        }

        private void LoadResources()
        {
            // Add tooltip images into the atlas
            UITextureAtlas atlas = GeneratedPanel.roadTooltipBox.Find<UISprite>("Sprite").atlas;
            if (atlas["Airplane Runway"] == null)
            {
                Texture2D[] textures = new Texture2D[]
                {
                    loadTextureFromAssembly("AirportRoads.Icons.runway_tooltip.png"),
                    loadTextureFromAssembly("AirportRoads.Icons.taxiway_tooltip.png")
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
				    "runway_default",
				    "runway_disabled",
				    "runway_hover",
				    "runway_pressed",
				    "runway_selected",
				    "taxiway_default",
				    "taxiway_disabled",
				    "taxiway_hover",
				    "taxiway_pressed",
				    "taxiway_selected"
			    };
                this.m_atlas = this.CreateTextureAtlas("AirportRoads", spriteNames, "AirportRoads.Icons.");
            }
        }

        private UITextureAtlas CreateTextureAtlas(string atlasName, string[] spriteNames, string assemblyPath)
        {
            int maxSize = 1024;
            Texture2D texture2D = new Texture2D(maxSize, maxSize, TextureFormat.ARGB32, false);
            Texture2D[] textures = new Texture2D[spriteNames.Length];
            Rect[] regions = new Rect[spriteNames.Length];

            for (int i = 0; i < spriteNames.Length; i++)
                textures[i] = this.loadTextureFromAssembly(assemblyPath + spriteNames[i] + ".png");

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
                    texture = texture2D,
                    region = regions[i]
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

            for (int i = 0; i < newTextures.Length; i++)
                textures[atlas.count + i] = newTextures[i];

            Rect[] regions = atlas.texture.PackTextures(textures, atlas.padding, 4096, false);

            atlas.sprites.Clear();

            for (int i = 0; i < textures.Length; i++)
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

            atlas.RebuildIndexes();
        }
    }
}