using ICities;

using ColossalFramework;
using ColossalFramework.Globalization;

using UnityEngine;

using System;
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

        public static readonly string version = "1.0";

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                InitMod();
            }
        }

        private void ShowNetwork(String name, String desc, GeneratedScrollPanel panel)
        {
            var netInfo = PrefabCollection<NetInfo>.FindLoaded(name);

            Locale locale = (Locale)typeof(LocaleManager).GetField("m_Locale", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SingletonLite<LocaleManager>.instance);
            Locale.Key key = new Locale.Key() { m_Identifier = "NET_TITLE", m_Key = name };
            if(!locale.Exists(key)) locale.AddLocalizedString(key, name);
            key = new Locale.Key() { m_Identifier = "NET_DESC", m_Key = name };
            if(!locale.Exists(key)) locale.AddLocalizedString(key, desc);

            typeof(GeneratedScrollPanel).GetMethod("CreateAssetItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { netInfo });
        }

        private void InitMod()
        {
            PublicTransportPanel ptp = GameObject.Find("PublicTransportPlanePanel").GetComponent<PublicTransportPanel>();
            ShowNetwork("Airplane Runway", "Runway", ptp);
            ShowNetwork("Airplane Taxiway", "Taxiway", ptp);
        }
    }
}