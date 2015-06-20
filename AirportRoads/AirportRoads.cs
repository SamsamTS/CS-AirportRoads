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

        public static readonly string version = "1.1";

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                LoadResources();
                InitMod();
            }
        }

        private void ShowNetwork(String name, String desc, GeneratedScrollPanel panel, string prefixIcon)
        {
            var netInfo = PrefabCollection<NetInfo>.FindLoaded(name);

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

        private void InitMod()
        {
            PublicTransportPanel ptp = GameObject.Find("PublicTransportPlanePanel").GetComponent<PublicTransportPanel>();
            ShowNetwork("Airplane Runway", "Runway", ptp, "runway_");
            ShowNetwork("Airplane Taxiway", "Taxiway", ptp, "taxiway_");
        }

        public UITextureAtlas m_atlas;

        public void LoadResources()
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
            this.m_atlas = this.CreateTextureAtlas("AirportRoads", UIView.GetAView().defaultAtlas.material, spriteNames, "AirportRoads.Icons.");
        }

        private UITextureAtlas CreateTextureAtlas(string atlasName, Material baseMaterial, string[] spriteNames, string assemblyPath)
        {
            int num = 1024;
            Texture2D texture2D = new Texture2D(num, num, TextureFormat.ARGB32, false);
            Texture2D[] array = new Texture2D[spriteNames.Length];
            Rect[] array2 = new Rect[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++)
            {
                array[i] = this.loadTextureFromAssembly(assemblyPath + spriteNames[i] + ".png", false);
            }
            array2 = texture2D.PackTextures(array, 2, num);
            UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Material material = UnityEngine.Object.Instantiate<Material>(baseMaterial);
            material.mainTexture = texture2D;
            uITextureAtlas.material = material;
            uITextureAtlas.name = atlasName;
            for (int i = 0; i < spriteNames.Length; i++)
            {
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo
                {
                    name = spriteNames[i],
                    texture = texture2D,
                    region = array2[i]
                };
                uITextureAtlas.AddSprite(item);
            }
            return uITextureAtlas;
        }

        private Texture2D loadTextureFromAssembly(string path, bool readOnly = true)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(path);
            byte[] array = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(array, 0, array.Length);
            Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            texture2D.LoadImage(array);
            texture2D.Apply(false, readOnly);
            return texture2D;
        }
    }
}