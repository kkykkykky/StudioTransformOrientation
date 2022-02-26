using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using System;
using UnityEngine;
#if AI || HS2
using AIChara;
#endif

namespace StudioTransformOrientation
{
#if AI || HS2
    [BepInProcess("StudioNEOV2")]
#else
    [BepInProcess("CharaStudio")]
#endif
    [BepInDependency(KKAPI.KoikatuAPI.GUID, KKAPI.KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, Game + " Studio Transform Orientation", Version)]
    public partial class StudioTransformOrientation : BaseUnityPlugin
    {
        public const string Version = "1.1.1";

        public static ConfigEntry<bool> ConfigChildRef;

        private static Studio.GuideObject.Mode guideObjectModeParent;
        private static Studio.GuideMove.MoveCalc guideMoveCalcParent;
        private static Studio.GuideObject.Mode guideObjectModeChild;
        private static Studio.GuideMove.MoveCalc guideMoveCalcChild;
        private static bool localActive = false;

        public static void Logging(BepInEx.Logging.LogLevel level, string _text)
        {
            BepInEx.Logging.Logger.CreateLogSource(nameof(StudioTransformOrientation)).Log(level, _text);
        }
        private void Main()
        {
            KKAPI.Studio.SaveLoad.StudioSaveLoadApi.SceneLoad += SceneLoaded;
            Harmony.CreateAndPatchAll(typeof(StudioTransformOrientation));
            ConfigChildRef.SettingChanged += delegate
            {
                if (localActive) SetExistingObjectsOrientation(localActive);
            };
        }

        private void Awake()
        {
            ConfigChildRef = Config.Bind("Child object transform orientation", "Relative to Parent", false, "When this option is Enabled and Local transform orientation is toggled on, move arrows orientation of child object will be relative to the rotaation of parent object instead of itself. Similar to HS1.");

            StudioAPI.StudioLoadedChanged += StudioAPI_Enter;
        }

        private void StudioAPI_Enter(object sender, EventArgs e)
        {
            Texture2D gIconTex = new Texture2D(32, 32);
            byte[] texData = ResourceUtils.GetEmbeddedResource("LocalTransBtn.png");
            gIconTex.LoadImage(texData);
            KKAPI.Studio.UI.CustomToolbarButtons.AddLeftToolbarToggle(gIconTex, false, active => {
                SetExistingObjectsOrientation(active);
            });
        }

        private static void SceneLoaded(object sender, SceneLoadEventArgs e)
        {
            if (localActive) SetExistingObjectsOrientation(localActive);
        }

        private static void SetExistingObjectsOrientation(bool setLocal)
        {
            localActive = setLocal;
            if (localActive)
            {
                guideObjectModeParent = GuideObject.Mode.LocalIK;
                guideMoveCalcParent = GuideMove.MoveCalc.TYPE3;
                guideMoveCalcChild = GuideMove.MoveCalc.TYPE3;
                if (ConfigChildRef.Value)
                {
                    guideObjectModeChild = GuideObject.Mode.Local;
                }
                else
                {
                    guideObjectModeChild = GuideObject.Mode.LocalIK;
                }
            }
            else
            {
                guideObjectModeParent = GuideObject.Mode.Local;
                guideMoveCalcParent = GuideMove.MoveCalc.TYPE1;
                guideObjectModeChild = GuideObject.Mode.World;
                guideMoveCalcChild = GuideMove.MoveCalc.TYPE2;
            }

            Studio.GuideObject[] _guideObjectsInScene = FindObjectsOfType<GuideObject>();
            if (!_guideObjectsInScene.IsNullOrEmpty())
            {
                foreach (var _guideObject in _guideObjectsInScene)
                {
                    if (_guideObject.parent)
                    {
                        _guideObject.mode = guideObjectModeChild;
                        Studio.GuideMove[] _guideMove = _guideObject.guideMove;
                        for (int i = 0; i < _guideMove.Length; i++)
                        {
                            _guideMove[i].moveCalc = guideMoveCalcChild;
                        }
                    }
                    else
                    {
                        _guideObject.mode = guideObjectModeParent;
                        Studio.GuideMove[] _guideMove = _guideObject.guideMove;
                        for (int i = 0; i < _guideMove.Length; i++)
                        {
                            _guideMove[i].moveCalc = guideMoveCalcParent;
                        }
                    }
                }

                Array.Clear(_guideObjectsInScene, 0, _guideObjectsInScene.Length);
            }
        }

        private static void SetNewObjectOrientation(GuideObject guideObject)
        {
            if (ConfigChildRef.Value && guideObject.parent)
            {
                guideObject.mode = GuideObject.Mode.Local;
            }
            else
            {
                guideObject.mode = GuideObject.Mode.LocalIK;
            }
            guideObject.moveCalc = GuideMove.MoveCalc.TYPE3;
        }

        private static void SetDetachedObjOrientation(GuideObject guideObject)
        {
            guideObject.mode = GuideObject.Mode.LocalIK;
            guideObject.moveCalc = GuideMove.MoveCalc.TYPE3;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Studio.OCICamera), "OnAttach")]
        [HarmonyPatch(typeof(Studio.OCIChar), "OnAttach")]
        [HarmonyPatch(typeof(Studio.OCIFolder), "OnAttach")]
        [HarmonyPatch(typeof(Studio.OCIItem), "OnAttach")]
        [HarmonyPatch(typeof(Studio.OCILight), "OnAttach")]
        [HarmonyPatch(typeof(Studio.OCIRoute), "OnAttach")]
        [HarmonyPatch(typeof(Studio.OCIRoutePoint), "OnAttach")]
        private static void SetNewChildOrientation(ObjectCtrlInfo _child)
        {
            if (localActive && _child != null)
            {
                GuideObject guideObject = _child.guideObject;
                SetNewObjectOrientation(guideObject);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Studio.OCICamera), "OnDetach")]
        [HarmonyPatch(typeof(Studio.OCIChar), "OnDetach")]
        [HarmonyPatch(typeof(Studio.OCIFolder), "OnDetach")]
        [HarmonyPatch(typeof(Studio.OCIItem), "OnDetach")]
        [HarmonyPatch(typeof(Studio.OCILight), "OnDetach")]
        [HarmonyPatch(typeof(Studio.OCIRoute), "OnDetach")]
        [HarmonyPatch(typeof(Studio.OCIRoutePoint), "OnDetach")]
        private static void SetDetachedObjOrientation(ObjectCtrlInfo __instance)
        {
            if (localActive)
            {
                GuideObject guideObject = __instance.guideObject;
                SetDetachedObjOrientation(guideObject);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Studio.AddObjectCamera), "Add")]
        [HarmonyPatch(typeof(Studio.AddObjectFolder), "Add")]
        [HarmonyPatch(typeof(Studio.AddObjectItem), "Add")]
        [HarmonyPatch(typeof(Studio.AddObjectLight), "Add")]
        [HarmonyPatch(typeof(Studio.AddObjectRoute), "Add")]
        [HarmonyPatch(typeof(Studio.AddObjectRoute), "AddPoint")]
        [HarmonyPatch(typeof(Studio.AddObjectRoute), "LoadPoint")]
        [HarmonyPatch(typeof(Studio.AddObjectCamera), "Load", new Type[] { typeof(OICameraInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
        [HarmonyPatch(typeof(Studio.AddObjectFolder), "Load", new Type[] { typeof(OIFolderInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
        [HarmonyPatch(typeof(Studio.AddObjectItem), "Load", new Type[] { typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
        [HarmonyPatch(typeof(Studio.AddObjectLight), "Load", new Type[] { typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
        [HarmonyPatch(typeof(Studio.AddObjectRoute), "Load", new Type[] { typeof(OIRouteInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
        private static void SetNewObjOrientation(ObjectCtrlInfo __result)
        {
            if (localActive && __result != null)
            {
                GuideObject guideObject = __result.guideObject;
                SetNewObjectOrientation(guideObject);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Studio.AddObjectFemale), "Load")]
        [HarmonyPatch(typeof(Studio.AddObjectMale), "Load")]
        [HarmonyPatch(typeof(Studio.AddObjectFemale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        [HarmonyPatch(typeof(Studio.AddObjectMale), "Add", new Type[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
        private static void SetNewCharaOrientation(OCIChar __result)
        {
            if (localActive && __result != null)
            {
                GuideObject guideObject = __result.guideObject;
                SetNewObjectOrientation(guideObject);
            }
        }
    }
}