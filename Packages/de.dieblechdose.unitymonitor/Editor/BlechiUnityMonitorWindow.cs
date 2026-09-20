using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
        BlechiLocalization.DrawLanguagePopup();
        EditorGUILayout.Space(6);

        DrawSystemStats();

        EditorGUILayout.Space(10);

        GUILayout.Label(
            BlechiLocalization.T("Avatar-Werte", "Avatar Stats"),
            EditorStyles.boldLabel);

        avatarRoot = (GameObject)EditorGUILayout.ObjectField(
            BlechiLocalization.T("Avatar-Root", "Avatar Root"),
            avatarRoot,
            typeof(GameObject),
            true
        );

        if (avatarRoot == null)
        {
            EditorGUILayout.HelpBox(
                BlechiLocalization.T(
                    "Zieh deinen Avatar-Root hier hinein, z. B. NovaBeastMawMainFBX Variant.",
                    "Drag your avatar root here, for example NovaBeastMawMainFBX Variant."),
                MessageType.Info);
        }
        else
        {
            DrawAvatarStats(avatarRoot);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button(BlechiLocalization.T(
            "Speicherbereinigung ausführen",
            "Collect Garbage")))
        {
            System.GC.Collect();
        }

        if (GUILayout.Button(BlechiLocalization.T(
            "Ungenutzte Assets entladen",
            "Unload Unused Assets")))
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        if (GUILayout.Button(BlechiLocalization.T(
            "Alle Objekte in der Szene aktivieren",
            "Enable All Objects In Scene")))
        {
            int changed = EnableAllObjectsInScene();

            EditorUtility.DisplayDialog(
                "Blechi Unity Monitor",
                BlechiLocalization.T(
                    changed + " deaktivierte Objekte wurden wieder aktiviert.",
                    changed + " disabled objects were enabled."),
                BlechiLocalization.T("Okay", "OK")
            );
        }
    }

    private void DrawSystemStats()
    {
        EditorGUILayout.LabelField(
            BlechiLocalization.T("Unity-Version", "Unity Version"),
            Application.unityVersion);
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
        EditorGUILayout.LabelField(
            BlechiLocalization.T("Dreiecke", "Triangles"),
            triangleCount.ToString("N0"));
        EditorGUILayout.LabelField(
            BlechiLocalization.T("Knochen / Transforms", "Bones / Transforms"),
            transforms.Length.ToString("N0"));
        EditorGUILayout.LabelField("Blendshapes", blendshapeCount.ToString("N0"));
        EditorGUILayout.LabelField(
            BlechiLocalization.T("Materialien", "Materials"),
            uniqueMaterials.Count.ToString());
        EditorGUILayout.LabelField(
            BlechiLocalization.T("Texturen", "Textures"),
            uniqueTextures.Count.ToString());
        EditorGUILayout.LabelField(
            BlechiLocalization.T("Geschätzter Texturspeicher", "Est. Texture Memory"),
            estimatedTextureMemory.ToString("F2") + " MB");
        EditorGUILayout.LabelField("PhysBones", physBones.ToString());
        EditorGUILayout.LabelField("PhysBone Colliders", physBoneColliders.ToString());
        EditorGUILayout.LabelField("Contacts", contacts.ToString());
        EditorGUILayout.LabelField("Constraints", constraints.ToString());
        EditorGUILayout.LabelField("Animators", animators.Length.ToString());

        EditorGUILayout.Space(8);

        DrawHealth(
            BlechiLocalization.T("Dreiecke", "Triangles"),
            triangleCount,
            70000,
            150000);
        DrawHealth(
            BlechiLocalization.T("Materialien", "Materials"),
            uniqueMaterials.Count,
            16,
            32);
        DrawHealth("Skinned Meshes", skinnedMeshes.Length, 16, 32);
        DrawHealth(
            BlechiLocalization.T("Texturspeicher", "Texture Memory"),
            estimatedTextureMemory,
            150f,
            300f);
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
            status = BlechiLocalization.T("ROT", "RED");
            type = MessageType.Error;
        }
        else if (value >= warning)
        {
            status = BlechiLocalization.T("GELB", "YELLOW");
            type = MessageType.Warning;
        }
        else
        {
            status = BlechiLocalization.T("GRÜN", "GREEN");
            type = MessageType.Info;
        }

        EditorGUILayout.HelpBox(label + ": " + status, type);
    }

    private static int EnableAllObjectsInScene()
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
