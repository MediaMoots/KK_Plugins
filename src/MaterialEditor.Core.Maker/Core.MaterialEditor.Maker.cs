using System;
using BepInEx;
using HarmonyLib;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using MaterialEditorAPI;
using System.Collections;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
using System.Collections.Generic;
using System.IO;
#if AI || HS2
using AIChara;
using ChaClothesComponent = AIChara.CmpClothes;
using ChaCustomHairComponent = AIChara.CmpHair;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// Plugin responsible for handling events from the character maker
    /// </summary>
#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#endif
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEMaker : MaterialEditorUI
    {
        /// <summary>
        /// MaterialEditor Maker plugin GUID
        /// </summary>
        public const string GUID = MaterialEditorPlugin.PluginGUID + ".maker";
        /// <summary>
        /// MaterialEditor Maker plugin name
        /// </summary>
        public const string PluginName = MaterialEditorPlugin.PluginName + " Maker";
        /// <summary>
        /// MaterialEditor Maker plugin version
        /// </summary>
        public const string Version = MaterialEditorPlugin.PluginVersion;
        /// <summary>
        /// Instance of the plugin
        /// </summary>
        public static MEMaker Instance;

        public static MakerButton MaterialEditorButton;
        internal static int currentHairIndex;
        internal static int currentClothesIndex;

        private void Start()
        {
            Instance = this;
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += (s, e) => ToggleButtonVisibility();
            MakerAPI.ReloadCustomInterface += (s, e) =>
            {
                StartCoroutine(Wait());
                IEnumerator Wait()
                {
                    yield return null;
                    ToggleButtonVisibility();
                }
            };
            MakerAPI.MakerExiting += (s, e) => ColorPalette = null;
            AccessoriesApi.SelectedMakerAccSlotChanged += (s, e) => ToggleButtonVisibility();
            AccessoriesApi.AccessoryKindChanged += (s, e) => ToggleButtonVisibility();
            AccessoriesApi.AccessoryTransferred += (s, e) => ToggleButtonVisibility();
#if KK || KKS
            AccessoriesApi.AccessoriesCopied  += (s,e)=> ToggleButtonVisibility();
#endif

            Harmony.CreateAndPatchAll(typeof(MakerHooks));
        }

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();

#if KK || EC || KKS
            MaterialEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Material Editor", null, this));
            MaterialEditorButton.GroupingID = "Buttons";
            MaterialEditorButton.OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter("face"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Face.All, this)).OnClick.AddListener(() => UpdateUICharacter());
            e.AddControl(new MakerButton("Export Colors for KKBP", MakerConstants.Face.All, this)).OnClick.AddListener(() => ExportColors());
            //e.AddControl(new MakerButton("Export All MC Masks for KKBP", MakerConstants.Face.All, this)).OnClick.AddListener(() => ExportMCMasks());


            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(() => UpdateUIClothes(6));
#if KK
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => UpdateUIClothes(8));
#elif KKS
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => UpdateUIClothes(8));
#elif EC
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(() => UpdateUIClothes(7));
#endif
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(() => UpdateUIHair(2));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(() => UpdateUIHair(3));

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(() => UpdateUICharacter("mayuge"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(() => UpdateUICharacter("eyeline,hitomi"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Nose, this)).OnClick.AddListener(() => UpdateUICharacter("nose"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("tang,tooth,canine"));
#endif

#if PH
            MaterialEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Material Editor", null, this));
            MaterialEditorButton.OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Material Editor (Body)", MakerConstants.Body.General, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Body.General, this)).OnClick.AddListener(() => UpdateUICharacter());
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Body.Nail, this)).OnClick.AddListener(() => UpdateUICharacter("nail"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Body.Lower, this)).OnClick.AddListener(() => UpdateUICharacter("mnpk"));

            e.AddControl(new MakerButton("Material Editor (Face)", MakerConstants.Face.General, this)).OnClick.AddListener(() => UpdateUICharacter("head,face"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Face.General, this)).OnClick.AddListener(() => UpdateUICharacter());
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eye, this)).OnClick.AddListener(() => UpdateUICharacter("eye"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyebrow, this)).OnClick.AddListener(() => UpdateUICharacter("mayuge"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyelash, this)).OnClick.AddListener(() => UpdateUICharacter("matuge"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("ha,sita"));

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Tops, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Bottoms, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Bra, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Shorts, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.SwimWear, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.SwimTops, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.SwimWear, this)).OnClick.AddListener(() => UpdateUIClothes(6));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Glove, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Panst, this)).OnClick.AddListener(() => UpdateUIClothes(8));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Socks, this)).OnClick.AddListener(() => UpdateUIClothes(9));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Wear.Shoes, this)).OnClick.AddListener(() => UpdateUIClothes(10));

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(() => UpdateUIHair(2));

#endif
            currentHairIndex = 0;
            currentClothesIndex = 0;

            ColorPalette = new MakerColorPalette();
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI || HS2
            MaterialEditorButton = MakerAPI.AddAccessoryWindowControl(new MakerButton("Material Editor", null, this));
            MaterialEditorButton.GroupingID = "Buttons";
            MaterialEditorButton.OnClick.AddListener(UpdateUIAccessory);
            e.AddControl(new MakerButton("Material Editor (Body)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("body"));
            e.AddControl(new MakerButton("Material Editor (Head)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter("head"));
            e.AddControl(new MakerButton("Material Editor (All)", MakerConstants.Body.All, this)).OnClick.AddListener(() => UpdateUICharacter());

            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Material Editor (Back)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(0));
            e.AddControl(new MakerButton("Material Editor (Front)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(1));
            e.AddControl(new MakerButton("Material Editor (Side)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(2));
            e.AddControl(new MakerButton("Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(() => UpdateUIHair(3));
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(0));
            e.AddControl(new MakerButton("Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(1));
            e.AddControl(new MakerButton("Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(2));
            e.AddControl(new MakerButton("Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(3));
            e.AddControl(new MakerButton("Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(4));
            e.AddControl(new MakerButton("Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(5));
            e.AddControl(new MakerButton("Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(6));
            e.AddControl(new MakerButton("Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(() => UpdateUIClothes(7));
            e.AddSubCategory(clothesCategory);

            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Mouth, this)).OnClick.AddListener(() => UpdateUICharacter("tang,tooth"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyes, this)).OnClick.AddListener(() => UpdateUICharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.HL, this)).OnClick.AddListener(() => UpdateUICharacter("eyebase,eyeshadow"));
            e.AddControl(new MakerButton("Material Editor", MakerConstants.Face.Eyelashes, this)).OnClick.AddListener(() => UpdateUICharacter("eyelashes"));
#endif
        }

        public static void ToggleButtonVisibility()
        {
            if (!MakerAPI.InsideMaker || MaterialEditorButton == null)
                return;

            var accessory = MakerAPI.GetCharacterControl().GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
            if (accessory == null)
            {
                MaterialEditorButton.Visible.OnNext(false);
            }
            else
            {
                MaterialEditorButton.Visible.OnNext(true);
            }
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the character or refreshes the UI if already open
        /// </summary>
        /// <param name="filter"></param>
        public void UpdateUICharacter(string filter = "")
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.gameObject, new ObjectData(0, MaterialEditorCharaController.ObjectType.Character), filter);
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the specified clothing index or refreshes the UI if already open
        /// </summary>
        /// <param name="index"></param>
        public void UpdateUIClothes(int index)
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

#if KK || KKS
            if (index > 8)
#elif PH
            if (index > 10)
#else
            if (index > 7)
#endif
            {
                Visible = false;
                return;
            }

            var chaControl = MakerAPI.GetCharacterControl();
            var clothes = chaControl.GetClothes(index);
#if PH
            if (clothes == null)
#else
            if (clothes == null || clothes.GetComponentInChildren<ChaClothesComponent>() == null)
#endif
                Visible = false;
            else
                PopulateList(clothes, new ObjectData(index, MaterialEditorCharaController.ObjectType.Clothing));
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the currently selected accesory or refreshes the UI if already open
        /// </summary>
        public void UpdateUIAccessory()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var accessory = MakerAPI.GetCharacterControl().GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot);
            if (accessory == null)
                Visible = false;
            else
                PopulateList(accessory, new ObjectData(AccessoriesApi.SelectedMakerAccSlot, MaterialEditorCharaController.ObjectType.Accessory));
        }

        public void ExportColors()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var chaControl = MakerAPI.GetCharacterControl();
            string jsonString = "";

            // Face Blush
            {
                var color1 = new Color32(0, 0, 0, 0);
                jsonString = jsonString + JsonUtility.ToJson(new ColorDataCustom("face_blush_color", "This value is in (KK Character Maker): Face->Makeup->Cheek Color", color1)) + ",\n";
            }

            // Get Body Colors
            {
                var charaGameObject = chaControl.gameObject;
                IEnumerable<Renderer> rendList = GetRendererList(charaGameObject);
                Dictionary<string, Material> matList = new Dictionary<string, Material>();

                foreach (var rend in rendList)
                {
                    foreach (var mat in GetMaterials(charaGameObject, rend))
                    {
                        matList[mat.NameFormatted()] = mat;
                    }
                }

                foreach (var mat in matList.Values)
                {
                    string materialName = mat.NameFormatted();
                    var color1 = mat.GetColor($"_Color");
                    var color2 = new Color32(0, 0, 0, 0);
                    var color3 = new Color32(0, 0, 0, 0);
                    var color4 = new Color32(0, 0, 0, 0);

                    if (materialName == "cf_m_mayuge_00" || materialName == "cf_m_eyeline_00_up" || materialName == "cf_m_tang"){
                        color1 = mat.GetColor($"_Color");
                        jsonString = jsonString + JsonUtility.ToJson(new ColorData(materialName, color1, color2, color3, color4)) + ",\n";
                    }
                    if (materialName == "cf_m_body"){
                        color1 = mat.GetColor($"_overcolor1");
                        jsonString = jsonString + JsonUtility.ToJson(new ColorData(materialName, color1, color2, color3, color4)) + ",\n";
                    }
                }
            }

            // Get Hair Colors
            {
                var hairGameObject = chaControl.GetHair()[0];
                IEnumerable<Renderer> rendList = GetRendererList(hairGameObject);
                Dictionary<string, Material> matList = new Dictionary<string, Material>();

                foreach (var rend in rendList)
                {
                    foreach (var mat in GetMaterials(hairGameObject, rend))
                    {
                        matList[mat.NameFormatted()] = mat;
                    }
                }

                foreach (var mat in matList.Values)
                {
                    string materialName = "Hair";
                    var color1 = mat.GetColor($"_Color");
                    var color2 = mat.GetColor($"_Color2");
                    var color3 = mat.GetColor($"_Color3");
                    var color4 = mat.GetColor($"_LineColor");

                    jsonString = jsonString + JsonUtility.ToJson(new ColorData(materialName, color1, color2, color3, color4)) + ",\n";
                    break;
                }
            }

            // Get Acc Colors
            var accessories = chaControl.GetAccessoryObjects();
            foreach (var acc in accessories)
            {
                IEnumerable<Renderer> rendList = GetRendererList(acc);
                Dictionary<string, Material> matList = new Dictionary<string, Material>();

                foreach (var rend in rendList)
                {
                    foreach (var mat in GetMaterials(acc, rend))
                    {
                        matList[mat.NameFormatted()] = mat;
                    }
                }

                foreach (var mat in matList.Values)
                {
                    string materialName = mat.NameFormatted();
                    var color1 = mat.GetColor($"_Color");
                    var color2 = mat.GetColor($"_Color2");
                    var color3 = mat.GetColor($"_Color3");
                    var color4 = new Color32(0, 0, 0, 0);
                    jsonString = jsonString + JsonUtility.ToJson(new ColorData(materialName, color1, color2, color3, color4)) + ",\n";
                }
            }

            // Cleanup Json
            jsonString = jsonString.Substring(0, jsonString.Length - 2);
            jsonString = '[' + jsonString + ']';

            //Console.WriteLine(jsonData);

            // Export Colors
            string exportFilePath = Path.Combine(MaterialEditorPluginBase.ExportPath, "KK_Colors.json");
            System.IO.File.WriteAllText(exportFilePath, jsonString);
            Utilities.OpenFileInExplorer(exportFilePath);
        }

/*        public void ExportMCMasks()
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            var chaControl = MakerAPI.GetCharacterControl();

            // Export Body MC Masks
            {
                var charaGameObject = chaControl.gameObject;
                IEnumerable<Renderer> rendList = GetRendererList(charaGameObject);
                Dictionary<string, Material> matList = new Dictionary<string, Material>();

                foreach (var rend in rendList)
                {
                    foreach (var mat in GetMaterials(charaGameObject, rend))
                    {
                        matList[mat.NameFormatted()] = mat;
                    }
                }

                foreach (var mat in matList.Values)
                {
                    if(mat.GetTexture("_ColorMask") != null)
                    {
                        ExportTexture(mat, "ColorMask");
                    }
                }
            }
        }*/

/*        private static void ExportTexture(Material mat, string property)
        {
            var tex = mat.GetTexture($"_{property}");
            if (tex == null) return;
            var matName = mat.NameFormatted();
            matName = string.Concat(matName.Split(Path.GetInvalidFileNameChars())).Trim();
            string filename = Path.Combine(MaterialEditorPluginBase.ExportPath, $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{matName}_{property}.png");
            SaveTex(tex, filename);
            MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
            Utilities.OpenFileInExplorer(filename);
        }*/

        /// <summary>
        /// Shows the MaterialEditor UI for the specified hair index or refreshes the UI if already open
        /// </summary>
        public void UpdateUIHair(int index)
        {
            if (!MakerAPI.InsideAndLoaded)
                return;

            if (index > 3)
            {
                Visible = false;
                return;
            }

            var chaControl = MakerAPI.GetCharacterControl();
            var hair = chaControl.GetHair(index);
#if PH
            if (hair == null)
#else
            if (hair.GetComponent<ChaCustomHairComponent>() == null)
#endif
                Visible = false;
            else
                PopulateList(hair, new ObjectData(index, MaterialEditorCharaController.ObjectType.Hair));
        }

        public override string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValueOriginal(objectData.Slot, objectData.ObjectType, renderer, property, go);
        }
        public override void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, value, go);
        }
        public override void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, go);
        }

        public override void MaterialCopyEdits(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialCopyEdits(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void MaterialPasteEdits(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialPasteEdits(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void MaterialCopyRemove(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).MaterialCopyRemove(objectData.Slot, objectData.ObjectType, material, go);
        }

        public override string GetMaterialShaderNameOriginal(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderOriginal(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void SetMaterialShaderName(object data, Material material, string value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShader(objectData.Slot, objectData.ObjectType, material, value, go);
        }
        public override void RemoveMaterialShaderName(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShader(objectData.Slot, objectData.ObjectType, material, go);
        }

        public override int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderRenderQueueOriginal(objectData.Slot, objectData.ObjectType, material, go);
        }
        public override void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, value, go);
        }
        public override void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, go);
        }

        public override bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureFromFile(objectData.Slot, objectData.ObjectType, material, propertyName, filePath, go, true);
        }
        public override void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTexture(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureOffsetOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTextureScaleOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialColorPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }

        public override float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            return MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialFloatPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
        public override void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).SetMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
        }
        public override void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject go)
        {
            ObjectData objectData = (ObjectData)data;
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
        }
    }
}

[System.Serializable]
public class ColorData{
    public string materialName;
    public Color32 color1;
    public Color32 color2;
    public Color32 color3;
    public Color32 color4;

    public ColorData(string materialName, Color color1, Color color2, Color color3, Color32 color4)
    {
        this.materialName = materialName;
        this.color1 = color1;
        this.color2 = color2;
        this.color3 = color3;
        this.color4 = color4;
    }
}

[System.Serializable]
public class ColorDataCustom{
    public string materialName;
    public string note;
    public Color32 color1;

    public ColorDataCustom(string materialName, string note, Color color1)
    {
        this.materialName = materialName;
        this.note = note;
        this.color1 = color1;
    }
}
