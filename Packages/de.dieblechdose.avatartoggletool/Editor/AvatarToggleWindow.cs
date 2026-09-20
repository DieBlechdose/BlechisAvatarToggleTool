using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public static class AvatarHierarchyIcons
{
    private const string ShowKey = "BlechiAvatarTools.ShowHierarchyIcons";
    private const string ActiveColorKey = "BlechiAvatarTools.ActiveColor";
    private const string InactiveColorKey = "BlechiAvatarTools.InactiveColor";
    private const string PositionKey = "BlechiAvatarTools.IconPosition";
    private const string SizeKey = "BlechiAvatarTools.IconSize";
    private const string ShowComponentIconsKey = "BlechiAvatarTools.ShowComponentIcons";
    private const string ComponentIconSizeKey = "BlechiAvatarTools.ComponentIconSize";
    private const string MaxComponentIconsKey = "BlechiAvatarTools.MaxComponentIcons";
    private const string ShowHierarchyLinesKey = "BlechiAvatarTools.ShowHierarchyLines";
    private const string HierarchyLineColorKey = "BlechiAvatarTools.HierarchyLineColor";
    private const string ComponentIconResourcePath =
        "BlechiAvatarTools/HierarchyIcons/";
    private const float HierarchyLevelWidth = 14f;
    private const float HierarchyBranchOffset = 22f;
    private const float HierarchyObjectGap = 5f;
    private const float HierarchyFoldoutGap = 16f;
    private const float HierarchyLineWidth = 1f;

    private static readonly Color DefaultHierarchyLineColor = EditorGUIUtility.isProSkin
        ? new Color(0.65f, 0.65f, 0.65f, 0.4f)
        : new Color(0.35f, 0.35f, 0.35f, 0.35f);

    private static readonly List<Component> ComponentBuffer = new List<Component>();
    private static readonly Dictionary<System.Type, Texture> ComponentIconCache =
        new Dictionary<System.Type, Texture>();
    private static readonly Dictionary<string, Texture2D> BundledIconCache =
        new Dictionary<string, Texture2D>();
    private static readonly Dictionary<System.Type, System.Reflection.PropertyInfo>
        ShapeTypePropertyCache =
            new Dictionary<System.Type, System.Reflection.PropertyInfo>();
    private static readonly Dictionary<System.Type, int> AlternateShapeValueCache =
        new Dictionary<System.Type, int>();
    private static readonly bool EnhancedHierarchyDetected =
        IsEnhancedHierarchyDetected();

    private static bool showHierarchyLines =
        EditorPrefs.GetBool(ShowHierarchyLinesKey, true);

    private static Color hierarchyLineColor =
        GetColor(HierarchyLineColorKey, DefaultHierarchyLineColor);

    static AvatarHierarchyIcons()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    public static bool ShowIcons
    {
        get => EditorPrefs.GetBool(ShowKey, true);
        set { EditorPrefs.SetBool(ShowKey, value); EditorApplication.RepaintHierarchyWindow(); }
    }

    public static Color ActiveColor
    {
        get => GetColor(ActiveColorKey, Color.green);
        set => SetColor(ActiveColorKey, value);
    }

    public static Color InactiveColor
    {
        get => GetColor(InactiveColorKey, Color.red);
        set => SetColor(InactiveColorKey, value);
    }

    public static bool IconOnLeft
    {
        get => EditorPrefs.GetBool(PositionKey, false);
        set { EditorPrefs.SetBool(PositionKey, value); EditorApplication.RepaintHierarchyWindow(); }
    }

    public static float IconSize
    {
        get => EditorPrefs.GetFloat(SizeKey, 16f);
        set { EditorPrefs.SetFloat(SizeKey, Mathf.Clamp(value, 8f, 24f)); EditorApplication.RepaintHierarchyWindow(); }
    }

    public static bool ShowComponentIcons
    {
        get => EditorPrefs.GetBool(ShowComponentIconsKey, true);
        set { EditorPrefs.SetBool(ShowComponentIconsKey, value); EditorApplication.RepaintHierarchyWindow(); }
    }

    public static float ComponentIconSize
    {
        get => EditorPrefs.GetFloat(ComponentIconSizeKey, 14f);
        set { EditorPrefs.SetFloat(ComponentIconSizeKey, Mathf.Clamp(value, 10f, 18f)); EditorApplication.RepaintHierarchyWindow(); }
    }

    public static int MaxComponentIcons
    {
        get => EditorPrefs.GetInt(MaxComponentIconsKey, 8);
        set { EditorPrefs.SetInt(MaxComponentIconsKey, Mathf.Clamp(value, 1, 12)); EditorApplication.RepaintHierarchyWindow(); }
    }

    public static bool ShowHierarchyLines
    {
        get => showHierarchyLines;
        set
        {
            if (showHierarchyLines == value) return;
            showHierarchyLines = value;
            EditorPrefs.SetBool(ShowHierarchyLinesKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    public static Color HierarchyLineColor
    {
        get => hierarchyLineColor;
        set
        {
            if (hierarchyLineColor == value) return;
            hierarchyLineColor = value;
            SetColor(HierarchyLineColorKey, value);
        }
    }

    public static bool IsEnhancedHierarchyInstalled => EnhancedHierarchyDetected;

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (EnhancedHierarchyDetected) return;
        if (!ShowIcons && !ShowComponentIcons && !ShowHierarchyLines) return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        if (ShowHierarchyLines && Event.current.type == EventType.Repaint)
        {
            DrawHierarchyLines(obj, selectionRect);
        }

        float rightEdge = selectionRect.xMax - 4f;

        if (ShowIcons)
        {
            DrawToggleIcon(obj, selectionRect, ref rightEdge);
        }

        if (ShowComponentIcons)
        {
            DrawComponentIcons(obj, selectionRect, ref rightEdge);
        }
    }

    private static void DrawHierarchyLines(GameObject obj, Rect selectionRect)
    {
        Transform item = obj.transform;
        Transform parent = item.parent;

        if (parent == null) return;

        float junctionY = Mathf.Round(selectionRect.center.y);
        float branchX = Mathf.Round(
            selectionRect.x - HierarchyBranchOffset);
        float connectorGap = item.childCount > 0
            ? HierarchyFoldoutGap
            : HierarchyObjectGap;
        float connectorEndX = selectionRect.x - connectorGap;

        DrawVerticalLine(
            branchX,
            selectionRect.yMin,
            HasNextSibling(item, parent)
                ? selectionRect.yMax
                : junctionY);
        DrawHorizontalLine(branchX, connectorEndX, junctionY);

        DrawAncestorBranches(
            parent,
            selectionRect,
            branchX - HierarchyLevelWidth);
    }

    private static void DrawAncestorBranches(
        Transform ancestor,
        Rect selectionRect,
        float branchX)
    {
        while (ancestor.parent != null)
        {
            Transform parent = ancestor.parent;

            if (HasNextSibling(ancestor, parent))
            {
                DrawVerticalLine(
                    branchX,
                    selectionRect.yMin,
                    selectionRect.yMax);
            }

            ancestor = parent;
            branchX -= HierarchyLevelWidth;
        }
    }

    private static bool HasNextSibling(Transform item, Transform parent)
    {
        return item.GetSiblingIndex() + 1 < parent.childCount;
    }

    private static void DrawVerticalLine(float x, float top, float bottom)
    {
        float roundedX = Mathf.Round(x);
        float roundedTop = Mathf.Round(Mathf.Min(top, bottom));
        float roundedBottom = Mathf.Round(Mathf.Max(top, bottom));

        EditorGUI.DrawRect(
            new Rect(
                roundedX,
                roundedTop,
                HierarchyLineWidth,
                roundedBottom - roundedTop),
            hierarchyLineColor);
    }

    private static void DrawHorizontalLine(float startX, float endX, float y)
    {
        float roundedY = Mathf.Round(y);
        float roundedStart = Mathf.Round(Mathf.Min(startX, endX));
        float roundedEnd = Mathf.Round(Mathf.Max(startX, endX));

        EditorGUI.DrawRect(
            new Rect(
                roundedStart,
                roundedY,
                roundedEnd - roundedStart,
                HierarchyLineWidth),
            hierarchyLineColor);
    }

    private static void DrawToggleIcon(GameObject obj, Rect selectionRect, ref float rightEdge)
    {
        float size = IconSize;
        float y = selectionRect.y + Mathf.Max(0f, (selectionRect.height - size) * 0.5f);

        Rect iconRect = IconOnLeft
            ? new Rect(selectionRect.x + 2f, y, size, size)
            : new Rect(rightEdge - size, y, size, size);

        if (!IconOnLeft)
        {
            rightEdge = iconRect.xMin - 2f;
        }

        Color oldColor = GUI.color;
        GUI.color = obj.activeSelf ? ActiveColor : InactiveColor;

        bool isActive = EditorGUI.Toggle(iconRect, obj.activeSelf);

        GUI.color = oldColor;

        if (isActive != obj.activeSelf)
        {
            Undo.RecordObject(
                obj,
                BlechiLocalization.T("GameObject aktivieren/deaktivieren", "Toggle GameObject Active"));
            obj.SetActive(isActive);
            EditorUtility.SetDirty(obj);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    private static void DrawComponentIcons(GameObject obj, Rect selectionRect, ref float rightEdge)
    {
        ComponentBuffer.Clear();
        obj.GetComponents(ComponentBuffer);

        int drawn = 0;
        float size = ComponentIconSize;
        float y = selectionRect.y + Mathf.Max(0f, (selectionRect.height - size) * 0.5f);
        float minimumX = selectionRect.x + 70f;

        for (int i = 0; i < ComponentBuffer.Count && drawn < MaxComponentIcons; i++)
        {
            Component component = ComponentBuffer[i];

            if (component is Transform) continue;
            if (rightEdge - size < minimumX) break;

            Texture iconTexture;
            string tooltip;

            if (component == null)
            {
                GUIContent missingIcon = EditorGUIUtility.IconContent("console.warnicon");
                iconTexture = missingIcon.image;
                tooltip = BlechiLocalization.T("Fehlendes Script", "Missing Script");
            }
            else
            {
                iconTexture = GetComponentIcon(component);
                tooltip = component.GetType().Name;
            }

            if (iconTexture == null) continue;

            Rect iconRect = new Rect(rightEdge - size, y, size, size);
            rightEdge = iconRect.xMin - 2f;

            Color oldColor = GUI.color;

            if (component is Behaviour behaviour && !behaviour.enabled)
            {
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * 0.4f);
            }

            GUI.Label(iconRect, new GUIContent(iconTexture, tooltip));
            GUI.color = oldColor;
            drawn++;

            if (component != null &&
                Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                iconRect.Contains(Event.current.mousePosition))
            {
                Selection.activeObject = component;
                EditorGUIUtility.PingObject(component);
                Event.current.Use();
                return;
            }
        }
    }

    private static Texture GetComponentIcon(Component component)
    {
        System.Type componentType = component.GetType();
        Texture2D bundledIcon = GetBundledComponentIcon(component, componentType);

        if (bundledIcon != null)
        {
            return bundledIcon;
        }

        if (ComponentIconCache.TryGetValue(componentType, out Texture cachedIcon))
        {
            return cachedIcon;
        }

        GUIContent objectContent =
            EditorGUIUtility.ObjectContent(component, componentType);
        Texture objectIcon = objectContent != null ? objectContent.image : null;

        if (IsSpecificComponentIcon(objectIcon))
        {
            ComponentIconCache[componentType] = objectIcon;
            return objectIcon;
        }

        if (component is MonoBehaviour monoBehaviour)
        {
            MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);

            if (monoScript != null)
            {
                Texture scriptIcon = EditorGUIUtility.GetIconForObject(monoScript);

                if (IsSpecificComponentIcon(scriptIcon))
                {
                    ComponentIconCache[componentType] = scriptIcon;
                    return scriptIcon;
                }

                Texture scriptThumbnail = AssetPreview.GetMiniThumbnail(monoScript);

                if (IsSpecificComponentIcon(scriptThumbnail))
                {
                    ComponentIconCache[componentType] = scriptThumbnail;
                    return scriptThumbnail;
                }
            }
        }

        GUIContent typeContent = EditorGUIUtility.ObjectContent(null, componentType);
        Texture typeIcon = typeContent != null ? typeContent.image : null;

        if (IsSpecificComponentIcon(typeIcon))
        {
            ComponentIconCache[componentType] = typeIcon;
            return typeIcon;
        }

        Texture typeThumbnail = AssetPreview.GetMiniTypeThumbnail(componentType);

        if (IsSpecificComponentIcon(typeThumbnail))
        {
            ComponentIconCache[componentType] = typeThumbnail;
            return typeThumbnail;
        }

        Texture objectThumbnail = AssetPreview.GetMiniThumbnail(component);

        if (IsSpecificComponentIcon(objectThumbnail))
        {
            ComponentIconCache[componentType] = objectThumbnail;
            return objectThumbnail;
        }

        ComponentIconCache[componentType] = null;
        return null;
    }

    private static Texture2D GetBundledComponentIcon(
        Component component,
        System.Type componentType)
    {
        string iconName = GetBundledIconName(component, componentType);
        if (string.IsNullOrEmpty(iconName)) return null;

        string themedName = EditorGUIUtility.isProSkin
            ? iconName
            : iconName + "_L";

        if (BundledIconCache.TryGetValue(themedName, out Texture2D cachedIcon))
        {
            return cachedIcon;
        }

        Texture2D icon = Resources.Load<Texture2D>(
            ComponentIconResourcePath + themedName);

        if (icon == null && themedName != iconName)
        {
            icon = Resources.Load<Texture2D>(
                ComponentIconResourcePath + iconName);
        }

        BundledIconCache[themedName] = icon;
        return icon;
    }

    private static string GetBundledIconName(
        Component component,
        System.Type componentType)
    {
        string typeName = componentType.Name;

        if (typeName == "UdonSharpBehaviour" ||
            (componentType.BaseType != null &&
             componentType.BaseType.Name == "UdonSharpBehaviour"))
        {
            return "vrcUdonSharpBehaviour";
        }

        switch (typeName)
        {
            case "VRCAvatarDescriptor":
                return "vrcAvatarDescriptor";
            case "VRCPerPlatformOverrides":
                return "vrcPerPlatformOverrides";
            case "VRCHeadChop":
                return "vrcHeadChop";
            case "VRCImpostorSettings":
            case "VRCImpostorEnvironment":
                return "vrcImpostorSettings";
            case "VRCRaycast":
                return "vrcRaycast";
            case "VRCSceneDescriptor":
                return "vrcSceneDescriptor";
            case "UdonBehaviour":
                return "vrcUdonBehaviour";
            case "VRCPickup":
                return "vrcPickup";
            case "VRCMirrorReflection":
                return "vrcMirrorReflection";
            case "VRCStation":
                return "vrcStation";
            case "VRCObjectSync":
                return "vrcObjectSync";
            case "VRCObjectPool":
                return "vrcObjectPool";
            case "VRCPortalMarker":
                return "vrcPortalMarker";
            case "VRCAvatarPedestal":
                return "vrcAvatarPedestal";
            case "VRCAVProVideoPlayer":
                return "vrcAVProVideoPlayer";
            case "VRCAVProVideoScreen":
                return "vrcAVProVideoScreen";
            case "VRCAVProVideoSpeaker":
                return "vrcAVProVideoSpeaker";
            case "VRCUiShape":
                return "vrcUiShape";
            case "VRCUnityVideoPlayer":
                return "vrcUnityVideoPlayer";
            case "VRCUrlInputField":
                return "vrcURLInputField";
            case "VRCCameraDollyAnimation":
                return "vrcCameraDollyAnimation";
            case "VRCCameraDollyPath":
                return "vrcCameraDollyPath";
            case "VRCCameraDollyPathPoint":
                return "vrcCameraDollyPoint";
            case "PipelineManager":
                return "vrcPipelineManager";
            case "VRCPhysBone":
                return "vrcPhysBone";
            case "VRCPhysBoneRoot":
                return "vrcPhysBoneRoot";
            case "VRCPhysBoneCollider":
                return UsesAlternateShapeIcon(component, "Plane")
                    ? "vrcPhysBoneColliderPlane"
                    : "vrcPhysBoneCollider";
            case "VRCContactReceiver":
                return UsesAlternateShapeIcon(component, "Box")
                    ? "vrcContactReceiverBox"
                    : "vrcContactReceiver";
            case "VRCContactSender":
                return UsesAlternateShapeIcon(component, "Box")
                    ? "vrcContactSenderBox"
                    : "vrcContactSender";
            case "VRCParentConstraint":
                return "vrcParentConstraint";
            case "VRCPositionConstraint":
                return "vrcPositionConstraint";
            case "VRCRotationConstraint":
                return "vrcRotationConstraint";
            case "VRCScaleConstraint":
                return "vrcScaleConstraint";
            case "VRCAimConstraint":
                return "vrcAimConstraint";
            case "VRCLookAtConstraint":
                return "vrcLookAtConstraint";
            case "VRCSpatialAudioSource":
                return "vrcSpatialAudioSource";
            case "VRCFury":
            case "VRCFuryComponent":
            case "UdonDiInjectField":
            case "UdonDiRegister":
                return "VRCFury";
            case "VRCFuryGlobalCollider":
                return "VRCFuryGlobalCollider";
            case "VRCFuryHapticPlug":
                return "VRCFurySPSPlug";
            case "VRCFuryHapticSocket":
                return "VRCFurySPSSocket";
            case "VRCFuryHapticTouchReceiver":
                return "VRCFuryHapticReceiver";
            case "VRCFuryHapticTouchSender":
                return "VRCFuryHapticSender";
            case "VRCFuryDebugInfo":
            case "VRCFuryTest":
                return "VRCFuryDebugInfo";
            case "BakeryPointLight":
                return "bakeryPointLight";
            case "BakeryLightMesh":
                return "bakeryLightMesh";
            case "BakeryDirectLight":
            case "BakerySkyLight":
                return "bakeryDirectLight";
            case "BakeryLightmapGroupSelector":
            case "BakeryLightmappedPrefab":
            case "BakeryPackAsSingleSquare":
            case "BakerySector":
            case "ftLightmapsStorage":
                return "bakeryGeneric";
            case "BakeryVolume":
                return "bakeryVolume";
            case "d4rkAvatarOptimizer":
                return "d4rkAvatarOptimizer";
            case "GestureManager":
                return "gestureManager";
            case "FaceEmoLauncherComponent":
            case "BlinkDisabler":
            case "TrackingControlDisabler":
            case "MenuRepositoryComponent":
            case "MenuRepositoryTestComponent":
            case "RestorationCheckpoint":
                return "FaceEmo";
            case "VRMMeta":
                return "vrmMeta";
            case "VRMBlendShapeProxy":
                return "vrmBlendShapeProxy";
            case "VRMSpringBone":
                return "vrmSpringBone";
            case "VRMSpringBoneColliderGroup":
                return "vrmSpringBoneColliderGroup";
            case "DynamicBone":
                return "dynamicBone";
            case "DynamicBoneCollider":
            case "DynamicBoneColliderBase":
                return "dynamicBoneCollider";
            case "DynamicBonePlaneCollider":
                return "dynamicBonePlaneCollider";
            default:
                return typeName.StartsWith(
                    "VRCFury",
                    System.StringComparison.Ordinal)
                        ? "VRCFury"
                        : null;
        }
    }

    private static bool UsesAlternateShapeIcon(
        Component component,
        string alternateShapeName)
    {
        System.Type componentType = component.GetType();

        if (!ShapeTypePropertyCache.TryGetValue(
                componentType,
                out System.Reflection.PropertyInfo shapeProperty))
        {
            shapeProperty = componentType.GetProperty(
                "shapeType",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            ShapeTypePropertyCache[componentType] = shapeProperty;
        }

        if (shapeProperty == null || !shapeProperty.PropertyType.IsEnum)
        {
            return false;
        }

        try
        {
            if (!AlternateShapeValueCache.TryGetValue(
                    componentType,
                    out int alternateShapeValue))
            {
                object alternateShape = System.Enum.Parse(
                    shapeProperty.PropertyType,
                    alternateShapeName);
                alternateShapeValue = System.Convert.ToInt32(alternateShape);
                AlternateShapeValueCache[componentType] = alternateShapeValue;
            }

            object shape = shapeProperty.GetValue(component, null);
            return shape != null &&
                   System.Convert.ToInt32(shape) == alternateShapeValue;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static bool IsEnhancedHierarchyDetected()
    {
        System.Reflection.Assembly[] assemblies =
            System.AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            if (assemblies[i].GetType(
                    "BluWizard.Hierarchy.BluHierarchy",
                    false) != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSpecificComponentIcon(Texture icon)
    {
        if (icon == null) return false;

        string iconName = icon.name;
        if (string.IsNullOrEmpty(iconName)) return false;

        return iconName.IndexOf("Script Icon", System.StringComparison.OrdinalIgnoreCase) < 0 &&
               iconName.IndexOf("DefaultAsset", System.StringComparison.OrdinalIgnoreCase) < 0 &&
               iconName.IndexOf("MonoBehaviour", System.StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static Color GetColor(string key, Color fallback)
    {
        string value = EditorPrefs.GetString(key, ColorUtility.ToHtmlStringRGBA(fallback));
        if (ColorUtility.TryParseHtmlString("#" + value, out Color color)) return color;
        return fallback;
    }

    private static void SetColor(string key, Color color)
    {
        EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
        EditorApplication.RepaintHierarchyWindow();
    }

    public static int EnableAllObjectsInScene()
    {
        int changed = 0;
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in children)
            {
                GameObject obj = t.gameObject;

                if (!obj.activeSelf)
                {
                    Undo.RecordObject(
                        obj,
                        BlechiLocalization.T("Alle Objekte aktivieren", "Enable All Objects"));
                    obj.SetActive(true);
                    EditorUtility.SetDirty(obj);
                    changed++;
                }
            }
        }

        EditorApplication.RepaintHierarchyWindow();
        return changed;
    }
}

public class AvatarToggleToolWindow : EditorWindow
{
    [MenuItem("Tools/Blechi Avatar Tools")]
    public static void Open()
    {
        GetWindow<AvatarToggleToolWindow>("Blechi Avatar Tools");
    }

    private static void DrawLanguageSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label(
            BlechiLocalization.T("Einstellungen", "Settings"),
            EditorStyles.boldLabel);

        BlechiLocalization.DrawLanguagePopup();

        EditorGUILayout.EndVertical();
    }

    private void OnGUI()
    {
        GUILayout.Label("Blechi Avatar Tools", EditorStyles.boldLabel);
        DrawLanguageSettings();
        EditorGUILayout.Space(8);

        if (AvatarHierarchyIcons.IsEnhancedHierarchyInstalled)
        {
            EditorGUILayout.HelpBox(
                BlechiLocalization.T(
                    "BluWizard Enhanced Hierarchy wurde erkannt. Die eigenen Hierarchy-Haken, Baumlinien und Komponenten-Icons sind deaktiviert, damit nichts doppelt angezeigt wird.",
                    "BluWizard Enhanced Hierarchy was detected. The built-in hierarchy toggles, relationship lines, and component icons are disabled to avoid duplicate displays."),
                MessageType.Info);
            EditorGUILayout.Space(8);
        }

        EditorGUI.BeginDisabledGroup(
            AvatarHierarchyIcons.IsEnhancedHierarchyInstalled);

        AvatarHierarchyIcons.ShowIcons = EditorGUILayout.Toggle(
            BlechiLocalization.T("Hierarchy-Haken anzeigen", "Show Hierarchy Toggles"),
            AvatarHierarchyIcons.ShowIcons);

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.ActiveColor = EditorGUILayout.ColorField(
            BlechiLocalization.T("Farbe aktiv", "Active Color"),
            AvatarHierarchyIcons.ActiveColor);
        AvatarHierarchyIcons.InactiveColor = EditorGUILayout.ColorField(
            BlechiLocalization.T("Farbe inaktiv", "Inactive Color"),
            AvatarHierarchyIcons.InactiveColor);

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.IconOnLeft = EditorGUILayout.Toggle(
            BlechiLocalization.T("Haken links", "Toggle On Left"),
            AvatarHierarchyIcons.IconOnLeft);
        AvatarHierarchyIcons.IconSize = EditorGUILayout.Slider(
            BlechiLocalization.T("Haken-Größe", "Toggle Size"),
            AvatarHierarchyIcons.IconSize,
            8f,
            24f);

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.ShowHierarchyLines = EditorGUILayout.Toggle(
            BlechiLocalization.T("Baumlinien anzeigen", "Show Hierarchy Lines"),
            AvatarHierarchyIcons.ShowHierarchyLines
        );

        if (AvatarHierarchyIcons.ShowHierarchyLines)
        {
            EditorGUI.indentLevel++;
            AvatarHierarchyIcons.HierarchyLineColor = EditorGUILayout.ColorField(
                BlechiLocalization.T("Farbe der Baumlinien", "Hierarchy Line Color"),
                AvatarHierarchyIcons.HierarchyLineColor
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.ShowComponentIcons = EditorGUILayout.Toggle(
            BlechiLocalization.T("Komponenten-Icons anzeigen", "Show Component Icons"),
            AvatarHierarchyIcons.ShowComponentIcons
        );

        if (AvatarHierarchyIcons.ShowComponentIcons)
        {
            EditorGUI.indentLevel++;
            AvatarHierarchyIcons.ComponentIconSize = EditorGUILayout.Slider(
                BlechiLocalization.T("Größe der Komponenten-Icons", "Component Icon Size"),
                AvatarHierarchyIcons.ComponentIconSize,
                10f,
                18f
            );
            AvatarHierarchyIcons.MaxComponentIcons = EditorGUILayout.IntSlider(
                BlechiLocalization.T("Max. Komponenten-Icons", "Max Component Icons"),
                AvatarHierarchyIcons.MaxComponentIcons,
                1,
                12
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.HelpBox(
            BlechiLocalization.T(
                "Klicke auf ein Komponenten-Icon, um die Komponente im Inspector auszuwählen.",
                "Click a component icon to select the component in the Inspector."),
            MessageType.Info
        );

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            BlechiLocalization.T(
                "Wichtig: Deaktivierte Objekte bleiben beim Upload unsichtbar. Vor dem Upload am besten alles wieder aktivieren.",
                "Important: Disabled objects remain invisible after upload. Enable everything again before uploading."),
            MessageType.Warning
        );

        if (GUILayout.Button(BlechiLocalization.T(
            "Alle Objekte in der Szene aktivieren",
            "Enable All Objects In Scene")))
        {
            if (EditorUtility.DisplayDialog(
                BlechiLocalization.T("Alles aktivieren?", "Enable everything?"),
                BlechiLocalization.T(
                    "Das aktiviert alle deaktivierten GameObjects in der aktuellen Szene.",
                    "This enables every disabled GameObject in the current scene."),
                BlechiLocalization.T("Ja", "Yes"),
                BlechiLocalization.T("Abbrechen", "Cancel")))
            {
                int changed = AvatarHierarchyIcons.EnableAllObjectsInScene();

                EditorUtility.DisplayDialog(
                    "Blechi Avatar Tools",
                    BlechiLocalization.T(
                        changed + " deaktivierte Objekte wurden wieder aktiviert.",
                        changed + " disabled objects were enabled."),
                    BlechiLocalization.T("Okay", "OK")
                );
            }
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button(BlechiLocalization.T(
            "Hierarchy neu zeichnen",
            "Repaint Hierarchy")))
        {
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
