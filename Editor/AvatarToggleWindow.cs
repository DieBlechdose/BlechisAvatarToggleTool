using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Diagnostics;

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
        get => EditorPrefs.GetBool(PositionKey, false);
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

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            iconRect.Contains(Event.current.mousePosition))
        {
            Undo.RecordObject(obj, "Toggle GameObject Active");
            obj.SetActive(!obj.activeSelf);
            EditorUtility.SetDirty(obj);

            EditorApplication.RepaintHierarchyWindow();
            Event.current.Use();
        }
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

    public static void EnableAllObjectsInScene()
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
                    Undo.RecordObject(obj, "Enable All Objects");
                    obj.SetActive(true);
                    EditorUtility.SetDirty(obj);
                    changed++;
                }
            }
        }

        EditorApplication.RepaintHierarchyWindow();

        EditorUtility.DisplayDialog(
            "Blechi Avatar Tools",
            $"Fertig. {changed} deaktivierte Objekte wurden wieder aktiviert.",
            "Okay"
        );
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

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Wichtig: Wenn du Objekte hier deaktivierst und den Avatar so hochlädst, bleiben diese Objekte im Upload unsichtbar. Vor dem Upload am besten alles wieder aktivieren.",
            MessageType.Warning
        );

        if (GUILayout.Button("Enable All Objects In Scene"))
        {
            if (EditorUtility.DisplayDialog(
                "Alles aktivieren?",
                "Das aktiviert alle deaktivierten GameObjects in der aktuellen Szene. Gut vor einem Upload.",
                "Ja, alles aktivieren",
                "Abbrechen"))
            {
                AvatarHierarchyIcons.EnableAllObjectsInScene();
            }
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Repaint Hierarchy"))
        {
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}

public class AvatarToggleMemoryWindow : EditorWindow
{
    private double lastTime;
    private float fps;

    [MenuItem("Tools/Blechi Unity Monitor")]
    public static void Open()
    {
        GetWindow<AvatarToggleMemoryWindow>("Blechi Unity Monitor");
    }

    private void OnEnable()
    {
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateStats;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateStats;
    }

    private void UpdateStats()
    {
        double now = EditorApplication.timeSinceStartup;
        double delta = now - lastTime;

        if (delta > 0)
        {
            fps = Mathf.Lerp(fps, (float)(1.0 / delta), 0.05f);
        }

        lastTime = now;
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("Blechi Unity Monitor", EditorStyles.boldLabel);

        long managedMemory = System.GC.GetTotalMemory(false);
        float managedMB = managedMemory / 1024f / 1024f;

        Process process = Process.GetCurrentProcess();
        float unityRAM = process.WorkingSet64 / 1024f / 1024f;

        EditorGUILayout.LabelField("Managed C# RAM", managedMB.ToString("F2") + " MB");
        EditorGUILayout.LabelField("Unity RAM", unityRAM.ToString("F2") + " MB");
        EditorGUILayout.LabelField("Editor FPS", fps.ToString("F1"));

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Collect Garbage"))
        {
            System.GC.Collect();
        }

        if (GUILayout.Button("Unload Unused Assets"))
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Zeigt grob Unity-Speicher und Editor-FPS. Das ist kein perfekter Benchmark, aber gut zum Beobachten.",
            MessageType.Info
        );
    }
}
