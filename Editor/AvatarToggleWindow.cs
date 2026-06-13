using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
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
                    Undo.RecordObject(obj, "Enable All Objects");
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

    private void OnGUI()
    {
        GUILayout.Label("Blechi Avatar Tools", EditorStyles.boldLabel);

        AvatarHierarchyIcons.ShowIcons = EditorGUILayout.Toggle("Show Hierarchy Icons", AvatarHierarchyIcons.ShowIcons);

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.ActiveColor = EditorGUILayout.ColorField("Active Color", AvatarHierarchyIcons.ActiveColor);
        AvatarHierarchyIcons.InactiveColor = EditorGUILayout.ColorField("Inactive Color", AvatarHierarchyIcons.InactiveColor);

        EditorGUILayout.Space(8);

        AvatarHierarchyIcons.IconOnLeft = EditorGUILayout.Toggle("Icon On Left", AvatarHierarchyIcons.IconOnLeft);
        AvatarHierarchyIcons.IconSize = EditorGUILayout.Slider("Icon Size", AvatarHierarchyIcons.IconSize, 8f, 24f);

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Wichtig: Deaktivierte Objekte bleiben beim Upload unsichtbar. Vor dem Upload am besten alles wieder aktivieren.",
            MessageType.Warning
        );

        if (GUILayout.Button("Enable All Objects In Scene"))
        {
            if (EditorUtility.DisplayDialog(
                "Alles aktivieren?",
                "Das aktiviert alle deaktivierten GameObjects in der aktuellen Szene.",
                "Ja",
                "Abbrechen"))
            {
                int changed = AvatarHierarchyIcons.EnableAllObjectsInScene();

                EditorUtility.DisplayDialog(
                    "Blechi Avatar Tools",
                    changed + " deaktivierte Objekte wurden wieder aktiviert.",
                    "Okay"
                );
            }
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Repaint Hierarchy"))
        {
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}

public class BlechiUnityMonitorWindow : EditorWindow
{
    private GameObject avatarRoot;

    [MenuItem("Tools/Blechi Unity Monitor")]
    public static void Open()
    {
        GetWindow<BlechiUnityMonitorWindow>("Blechi Unity Monitor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Blechi Unity Monitor", EditorStyles.boldLabel);

        DrawSystemStats();

        EditorGUILayout.Space(10);

        GUILayout.Label("Avatar Stats", EditorStyles.boldLabel);

        avatarRoot = (GameObject)EditorGUILayout.ObjectField(
            "Avatar Root",
            avatarRoot,
            typeof(GameObject),
            true
        );

        if (avatarRoot == null)
        {
            EditorGUILayout.HelpBox("Zieh deinen Avatar Root hier rein, z.B. NovaBeastMawMainFBX Variant.", MessageType.Info);
        }
        else
        {
            DrawAvatarStats(avatarRoot);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Collect Garbage"))
        {
            System.GC.Collect();
        }

        if (GUILayout.Button("Unload Unused Assets"))
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        if (GUILayout.Button("Enable All Objects In Scene"))
        {
            int changed = AvatarHierarchyIcons.EnableAllObjectsInScene();

            EditorUtility.DisplayDialog(
                "Blechi Unity Monitor",
                changed + " deaktivierte Objekte wurden wieder aktiviert.",
                "Okay"
            );
        }
    }

    private void DrawSystemStats()
    {
        long managedMemory = System.GC.GetTotalMemory(false);
        float managedMB = managedMemory / 1024f / 1024f;

        float privateRAM = 0f;
        float workingRAM = 0f;

        try
        {
            Process process = Process.GetCurrentProcess();
            privateRAM = process.PrivateMemorySize64 / 1024f / 1024f;
            workingRAM = process.WorkingSet64 / 1024f / 1024f;
        }
        catch
        {
            privateRAM = -1f;
            workingRAM = -1f;
        }

        EditorGUILayout.LabelField("Unity Version", Application.unityVersion);
        EditorGUILayout.LabelField("Managed C# RAM", managedMB.ToString("F2") + " MB");

        if (privateRAM >= 0)
        {
            EditorGUILayout.LabelField("Unity Private RAM", privateRAM.ToString("F2") + " MB");
            EditorGUILayout.LabelField("Unity Working RAM", workingRAM.ToString("F2") + " MB");
        }
        else
        {
            EditorGUILayout.LabelField("Unity RAM", "Nicht verfügbar");
        }
    }

    private void DrawAvatarStats(GameObject root)
    {
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        SkinnedMeshRenderer[] skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);

        int triangleCount = 0;
        int blendshapeCount = 0;

        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        HashSet<Texture> uniqueTextures = new HashSet<Texture>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh != null)
                triangleCount += CountTriangles(mf.sharedMesh);
        }

        foreach (SkinnedMeshRenderer smr in skinnedMeshes)
        {
            if (smr.sharedMesh != null)
            {
                triangleCount += CountTriangles(smr.sharedMesh);
                blendshapeCount += smr.sharedMesh.blendShapeCount;
            }
        }

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.sharedMaterials)
            {
                if (mat == null) continue;

                uniqueMaterials.Add(mat);

                Shader shader = mat.shader;
                if (shader == null) continue;

                int propCount = ShaderUtil.GetPropertyCount(shader);

                for (int i = 0; i < propCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = mat.GetTexture(propName);

                        if (tex != null)
                            uniqueTextures.Add(tex);
                    }
                }
            }
        }

        int physBones = CountComponentsByName(root, "VRCPhysBone");
        int physBoneColliders = CountComponentsByName(root, "VRCPhysBoneCollider");
        int contacts = CountComponentsByName(root, "Contact");
        int constraints = CountComponentsByName(root, "Constraint");

        float estimatedTextureMemory = EstimateTextureMemoryMB(uniqueTextures);

        EditorGUILayout.LabelField("GameObjects", transforms.Length.ToString());
        EditorGUILayout.LabelField("Mesh Filters", meshFilters.Length.ToString());
        EditorGUILayout.LabelField("Skinned Meshes", skinnedMeshes.Length.ToString());
        EditorGUILayout.LabelField("Renderers", renderers.Length.ToString());
        EditorGUILayout.LabelField("Triangles", triangleCount.ToString("N0"));
        EditorGUILayout.LabelField("Bones / Transforms", transforms.Length.ToString("N0"));
        EditorGUILayout.LabelField("Blendshapes", blendshapeCount.ToString("N0"));
        EditorGUILayout.LabelField("Materials", uniqueMaterials.Count.ToString());
        EditorGUILayout.LabelField("Textures", uniqueTextures.Count.ToString());
        EditorGUILayout.LabelField("Est. Texture Memory", estimatedTextureMemory.ToString("F2") + " MB");
        EditorGUILayout.LabelField("PhysBones", physBones.ToString());
        EditorGUILayout.LabelField("PhysBone Colliders", physBoneColliders.ToString());
        EditorGUILayout.LabelField("Contacts", contacts.ToString());
        EditorGUILayout.LabelField("Constraints", constraints.ToString());
        EditorGUILayout.LabelField("Animators", animators.Length.ToString());

        EditorGUILayout.Space(8);

        DrawHealth("Triangles", triangleCount, 70000, 150000);
        DrawHealth("Materials", uniqueMaterials.Count, 16, 32);
        DrawHealth("Skinned Meshes", skinnedMeshes.Length, 16, 32);
        DrawHealth("Texture Memory", estimatedTextureMemory, 150f, 300f);
        DrawHealth("PhysBones", physBones, 16, 32);
    }

    private int CountTriangles(Mesh mesh)
    {
        int count = 0;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            count += mesh.GetTriangles(i).Length / 3;
        }

        return count;
    }

    private int CountComponentsByName(GameObject root, string namePart)
    {
        int count = 0;
        Component[] components = root.GetComponentsInChildren<Component>(true);

        foreach (Component c in components)
        {
            if (c == null) continue;

            string typeName = c.GetType().Name;

            if (typeName.Contains(namePart))
                count++;
        }

        return count;
    }

    private float EstimateTextureMemoryMB(HashSet<Texture> textures)
    {
        long total = 0;

        foreach (Texture tex in textures)
        {
            if (tex == null) continue;

            int width = tex.width;
            int height = tex.height;

            total += (long)width * height * 4;
        }

        return total / 1024f / 1024f;
    }

    private void DrawHealth(string label, float value, float warning, float danger)
    {
        string status;
        MessageType type;

        if (value >= danger)
        {
            status = "ROT";
            type = MessageType.Error;
        }
        else if (value >= warning)
        {
            status = "GELB";
            type = MessageType.Warning;
        }
        else
        {
            status = "GRÜN";
            type = MessageType.Info;
        }

        EditorGUILayout.HelpBox(label + ": " + status, type);
    }
}
