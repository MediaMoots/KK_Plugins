﻿using BepInEx;
using BepInEx.Logging;
using KKAPI.Maker;
using KKAPI.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class LightingTweaks : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.lightingtweaks";
        public const string PluginName = "Lighting Tweaks";
        public const string PluginNameInternal = "KK_LightingTweaks";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        internal void Main()
        {
            Logger = base.Logger;

            MakerAPI.MakerFinishedLoading += (s, e) => TweakLighting();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void TweakLighting()
        {
            foreach (var light in FindObjectsOfType<Light>())
            {
                light.shadowCustomResolution = 10000;
                light.shadowBias = 0.0075f;
                if (light.name == "Directional Chara") //Studio shadow strength is different from main game for some reason
                    light.shadowStrength = 1;
            }
        }

        private void SceneManager_sceneLoaded(Scene s, LoadSceneMode lsm)
        {
            if (StudioAPI.InsideStudio)
                TweakLighting();
        }
    }
}