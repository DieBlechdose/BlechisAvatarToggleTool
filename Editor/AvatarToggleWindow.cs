using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AvatarHierarchyToggles
{
    private const string EnabledKey = "AvatarToggleTool.ShowHierarchyToggles";

    static AvatarHierarchyToggles()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    public static bool ShowToggles
    {
        get => EditorPrefs.GetBool(EnabledKey, true);
        set
        {
            EditorPrefs.SetBool(EnabledKey, value);
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (!ShowToggles) return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        Rect toggleRect = new Rect(selectionRect.xMax - 20, selectionRect.y, 18, selectionRect.height);

        bool active = obj.activeSelf;
        bool newActive = GUI.Toggle(toggleRect, active, GUIContent.none);

        if (newActive != active)
        {
            Undo.RecordObject(obj, "Toggle GameObject Active");
            obj.SetActive(newActive);
            EditorUtility.SetDirty(obj);
        }
    }
}

public class AvatarToggleWindow : EditorWindow
{
    [MenuItem("Tools/Avatar Toggle Tool")]
    public static void Open()
    {
        GetWindow<AvatarToggleWindow>("Avatar Toggles");
    }

    private void OnGUI()
    {
        GUILayout.Label("Avatar Toggle Tool", EditorStyles.boldLabel);

        AvatarHierarchyToggles.ShowToggles = EditorGUILayout.Toggle(
            "Show Hierarchy Toggles",
            AvatarHierarchyToggles.ShowToggles
        );

        EditorGUILayout.HelpBox(
            "Wenn aktiv, erscheinen Checkboxen direkt rechts in der Hierarchy.",
            MessageType.Info
        );
    }
}
