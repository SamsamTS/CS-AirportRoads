using ICities;

using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Globalization;

using UnityEngine;

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Detour;


namespace AirportRoads
{
    public class AirportRoads : ThreadingExtensionBase, IUserMod
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

        public const string version = "1.3.0";

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (m_panelGameObject == null)
            {
                m_panelGameObject = GameObject.Find("PublicTransportPlanePanel");

                if (m_panelGameObject == null)
                {
                    m_panelGameObject = GameObject.Find("LandscapingPathsPanel");

                    if (m_panelGameObject == null) return;
                }

                DebugUtils.Log(m_panelGameObject.name + " found.");

                try
                {
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

        private GameObject m_panelGameObject;

        private UITextureAtlas m_atlas;

        private void InitMod()
        {
            try
            {
                // StreetDirectionViewer support
                InitSDV();
            }
            catch (Exception e)
            {
                DebugUtils.Log(e.Message);
            }

            GeneratedScrollPanel panel = m_panelGameObject.GetComponent<GeneratedScrollPanel>();
            panel.RefreshPanel();

            ShowNetwork("Airplane Runway", "Runway", panel, 7000, 600, "Runway");
            ShowNetwork("Airplane Taxiway", "Taxiway", panel, 4000, 200, "Taxiway");

        }

        private void ShowNetwork(string name, string desc, GeneratedScrollPanel panel, int constructionCost, int maintenanceCost, string prefixIcon)
        {
            if (panel.Find<UIButton>(name) != null) return;

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
            if(!locale.Exists(key)) locale.AddLocalizedString(key, name);
            key = new Locale.Key() { m_Identifier = "NET_DESC", m_Key = name };
            if(!locale.Exists(key)) locale.AddLocalizedString(key, desc);

            typeof(GeneratedScrollPanel).GetMethod("CreateAssetItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { netInfo });
        }

        private void LoadResources()
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

        #region StreetDirectionViewer support
        private void InitSDV()
        {
            Type ArrowManager = Type.GetType("StreetDirectionViewer.ArrowManager, StreetDirectionViewer");
            // SDV installed ?
            if (ArrowManager == null) return;

            // Getting StreetDirectionViewerUI instance
            object instance = null;
            FieldInfo field = typeof(ThreadingWrapper).GetField("m_ThreadingExtensions", BindingFlags.NonPublic | BindingFlags.Instance);
            List<IThreadingExtension> threadingExtensions = field.GetValue(Singleton<SimulationManager>.instance.m_ThreadingWrapper) as List<IThreadingExtension>;
            Type ThreadingExtension = TryGetType("StreetDirectionViewer.ThreadingExtension, StreetDirectionViewer");

            for (int i = 0; i < threadingExtensions.Count; i++)
            {
                if (threadingExtensions[i].GetType() == ThreadingExtension)
                {
                    field = TryGetField(ThreadingExtension, "streetDirectionViewerUI", BindingFlags.NonPublic | BindingFlags.Instance);
                    instance = field.GetValue(threadingExtensions[i]);
                    DebugUtils.Log("StreetDirectionViewerUI instance found");
                    break;
                }
            }

            // SDV enabled ?
            if (instance == null) return;
            DebugUtils.Log("StreetDirectionViewer mod detected. Adding support.");

            // Getting arrowManager
            Type StreetDirectionViewerUI = TryGetType("StreetDirectionViewer.StreetDirectionViewerUI, StreetDirectionViewer");
            field = TryGetField(StreetDirectionViewerUI, "arrowManager", BindingFlags.NonPublic | BindingFlags.Instance);
            object arrowManager = field.GetValue(instance);

            // Getting Update
            MethodInfo Update = TryGetMethod(ArrowManager, "Update", BindingFlags.Public | BindingFlags.Instance);

            // Getting CreateButton
            MethodInfo CreateButton = TryGetMethod(StreetDirectionViewerUI, "CreateButton", BindingFlags.NonPublic | BindingFlags.Instance);

            // Getting setShowStreetDirectionButtonState
            MethodInfo ShowStreetDirection = TryGetMethod(StreetDirectionViewerUI, "setShowStreetDirectionButtonState", BindingFlags.Public | BindingFlags.Instance);


            // IsAirstripOrHarbor redirect
            MethodInfo from = TryGetMethod(ArrowManager, "IsAirstripOrHarbor", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo to = typeof(AirportRoads).GetMethod("IsAirstripOrHarbor", BindingFlags.NonPublic | BindingFlags.Static);
            Detour.RedirectCallsState state = null;


            UIPanel optionsBar = GameObject.Find("OptionsBar").GetComponent<UIPanel>();

            if (optionsBar == null) return;

            optionsBar.eventComponentAdded += (c, component) =>
            {
                try
                {
                    if (m_panelGameObject.GetComponent<UIPanel>().isVisible && component.name.StartsWith("RoadsOptionPanel"))
                    {
                        DebugUtils.Log("RoadsOptionPanel added. Calling SDV CreateButton");

                        if ((bool)CreateButton.Invoke(instance, new object[] { component }))
                        {
                            DebugUtils.Log("SDV button created");

                            ShowStreetDirection.Invoke(instance, new object[] { true });

                            component.eventVisibilityChanged += (d, visible) =>
                            {
                                if (visible)
                                {
                                    state = RedirectionHelper.RedirectCalls(from, to);
                                    Update.Invoke(arrowManager, null);
                                }
                                else if (state != null)
                                {
                                    RedirectionHelper.RevertRedirect(from, state);
                                    state = null;
                                }
                            };
                        }
                    }
                }
                catch(Exception e)
                {
                    DebugUtils.Log(e.Message);
                }
            };
        }

        private static Type TryGetType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null) throw new Exception ("Couldn't find the type "+typeName);

            return type;
        }

        private static MethodInfo TryGetMethod(Type type, string name, BindingFlags flags)
        {
            MethodInfo method = type.GetMethod(name, flags);
            if (method == null) throw new Exception("Couldn't find the method " + name);

            return method;
        }

        private static FieldInfo TryGetField(Type type, string name, BindingFlags flags)
        {
            FieldInfo field = type.GetField(name, flags);
            if (field == null) throw new Exception("Couldn't find the field " + name);

            return field;
        }

        private static bool IsAirstripOrHarbor(NetSegment segment)
        {
            NetInfo.Lane[] lanes = segment.Info.m_lanes;
            
            for (int i = 0; i < lanes.Length; i++)
            {
                NetInfo.Lane lane = lanes[i];
                if (lane.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Plane))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}