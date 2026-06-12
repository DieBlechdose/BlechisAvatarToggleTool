using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AvatarHierarchyIcons
{
    private const string ShowKey = "BlechiAvatarTools.ShowHierarchyIcons";
    private const string ActiveColorKey = "BlechiAvatarTools.ActiveColor";
    private const string InactiveColorKey = "BlechiAvatarTools.InactiveColor";
    private const string PositionKey = "BlechiAvatarTools.IconPosition";
    private const string SizeKey = "BlechiAvatarTools.IconSize";

    static AvatarHierarchyIcons()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    public static bool ShowIcons
    {
        get => EditorPrefs.GetBool(ShowKey, true);
        set
        {
            EditorPrefs.SetBool(ShowKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
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
        get => EditorPrefs.GetBool(PositionKey, true);
        set
        {
            EditorPrefs.SetBool(PositionKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    public static float IconSize
    {
        get => EditorPrefs.GetFloat(SizeKey, 16f);
        set
        {
            EditorPrefs.SetFloat(SizeKey, Mathf.Clamp(value, 8f, 24f));
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (!ShowIcons) return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        float size = IconSize;

        Rect iconRect = IconOnLeft
            ? new Rect(selectionRect.x + 2, selectionRect.y + 1, size, size)
            : new Rect(selectionRect.xMax - size - 4, selectionRect.y + 1, size, size);

        Color oldColor = GUI.color;
        GUI.color = obj.activeSelf ? ActiveColor : InactiveColor;

        GUIContent icon = EditorGUIUtility.IconContent("GameObject Icon");
        GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit, true);

        GUI.color = oldColor;
    }

    private static Color GetColor(string key, Color fallback)
    {
        string value = EditorPrefs.GetString(key, ColorUtility.ToHtmlStringRGBA(fallback));

        if (ColorUtility.TryParseHtmlString("#" + value, out Color color))
            return color;

        return fallback;
    }

    private static void SetColor(string key, Color color)
    {
        EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
        EditorApplication.RepaintHierarchyWindow();
    }
}

public class AvatarToggleToolWindow : EditorWindow
{
    [MenuItem("Tools/Blechi Avatar Tools")]
    public static void Open()
    {
        GetWindow<AvatarToggleToolWindow>("Blechi Avatar Tools");
    }

    private void OnGUI()
    {
        GUILayout.Label("Blechi Avatar Tools", EditorStyles.boldLabel);

        AvatarHierarchyIcons.ShowIcons = EditorGUILayout.Toggle(
            "Show Hierarchy Icons",
            AvatarHierarchyIcons.ShowIcons
        );

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.ActiveColor = EditorGUILayout.ColorField(
            "Active Color",
            AvatarHierarchyIcons.ActiveColor
        );

        AvatarHierarchyIcons.InactiveColor = EditorGUILayout.ColorField(
            "Inactive Color",
            AvatarHierarchyIcons.InactiveColor
        );

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.IconOnLeft = EditorGUILayout.Toggle(
            "Icon On Left",
            AvatarHierarchyIcons.IconOnLeft
        );

        AvatarHierarchyIcons.IconSize = EditorGUILayout.Slider(
            "Icon Size",
            AvatarHierarchyIcons.IconSize,
            8f,
            24f
        );

        EditorGUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Zeigt farbige GameObject-Icons direkt in der Hierarchy. Aktiv = Active Color, aus = Inactive Color.",
            MessageType.Info
        );
    }
}
