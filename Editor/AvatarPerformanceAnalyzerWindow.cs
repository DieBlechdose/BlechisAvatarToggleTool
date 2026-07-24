using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class AvatarPerformanceAnalyzerWindow : EditorWindow
{
    private enum PerformanceRating
    {
        Excellent = 0,
        Good = 1,
        Medium = 2,
        Poor = 3,
        VeryPoor = 4
    }

    private sealed class MetricDefinition
    {
        public readonly string Name;
        public readonly double[] Limits;
        public readonly string Unit;
        public readonly bool WholeNumber;

        public MetricDefinition(
            string name,
            double excellent,
            double good,
            double medium,
            double poor,
            string unit = "",
            bool wholeNumber = true)
        {
            Name = name;
            Limits = new[] { excellent, good, medium, poor };
            Unit = unit;
            WholeNumber = wholeNumber;
        }
    }

    private sealed class MetricResult
    {
        public MetricDefinition Definition;
        public double Value;
        public PerformanceRating Rating;
    }

    private static readonly MetricDefinition[] Definitions =
    {
        new MetricDefinition("Triangles", 32000, 70000, 70000, 70000),
        new MetricDefinition("Texture Memory", 40, 75, 110, 150, "MB", false),
        new MetricDefinition("Material Slots", 4, 8, 16, 32),
        new MetricDefinition("Skinned Meshes", 1, 2, 8, 16),
        new MetricDefinition("PhysBones", 4, 8, 16, 32),
        new MetricDefinition("PhysBone Transforms", 16, 64, 128, 256),
        new MetricDefinition("PhysBone Colliders", 4, 8, 16, 32),
        new MetricDefinition("Contacts", 8, 16, 24, 32),
        new MetricDefinition("Bones", 75, 150, 256, 400),
        new MetricDefinition("Animators", 1, 4, 16, 32)
    };

    private readonly List<MetricResult> results = new List<MetricResult>();
    private readonly bool[] foldouts = { true, true, true, true, true };

    private GameObject avatarRoot;
    private Component avatarDescriptor;
    private Vector2 scrollPosition;
    private PerformanceRating overallRating = PerformanceRating.Excellent;
    private string validationMessage;
    private int lastResultSignature;
    private double nextAutomaticAnalysis;

    [MenuItem("Tools/Avatar Performance Analyzer")]
    public static void Open()
    {
        GetWindow<AvatarPerformanceAnalyzerWindow>("Avatar Performance Analyzer");
    }

    private void OnEnable()
    {
        minSize = new Vector2(440f, 480f);
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawAvatarSelection();
        DrawButtons();

        EditorGUILayout.Space(8f);

        if (!string.IsNullOrEmpty(validationMessage))
        {
            EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
        }

        if (avatarRoot == null || results.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Ziehe das GameObject mit dem VRCAvatarDescriptor in das Feld und starte die Analyse.",
                MessageType.Info
            );
            return;
        }

        DrawOverallRating();

        EditorGUILayout.Space(6f);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawRatingFoldout(PerformanceRating.VeryPoor);
        DrawRatingFoldout(PerformanceRating.Poor);
        DrawRatingFoldout(PerformanceRating.Medium);
        DrawRatingFoldout(PerformanceRating.Good);
        DrawRatingFoldout(PerformanceRating.Excellent);

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        GUILayout.Label("Avatar Performance Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Analysiert den ausgewählten VRChat-Avatar anhand der hinterlegten Performance-Grenzwerte.",
            EditorStyles.wordWrappedMiniLabel
        );
        EditorGUILayout.Space(6f);
    }

    private void DrawAvatarSelection()
    {
        EditorGUI.BeginChangeCheck();
        GameObject newAvatar = (GameObject)EditorGUILayout.ObjectField(
            "Avatar",
            avatarRoot,
            typeof(GameObject),
            true
        );

        if (EditorGUI.EndChangeCheck())
        {
            avatarRoot = newAvatar;
            AnalyzeAvatar(true);
        }
    }

    private void DrawButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Analyze Avatar"))
        {
            AnalyzeAvatar(true);
        }

        if (GUILayout.Button("Clear", GUILayout.Width(90f)))
        {
            ClearAnalysis();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawOverallRating()
    {
        MessageType messageType;

        switch (overallRating)
        {
            case PerformanceRating.VeryPoor:
            case PerformanceRating.Poor:
                messageType = MessageType.Error;
                break;
            case PerformanceRating.Medium:
                messageType = MessageType.Warning;
                break;
            default:
                messageType = MessageType.Info;
                break;
        }

        EditorGUILayout.HelpBox(
            "Gesamtbewertung: " + GetRatingLabel(overallRating),
            messageType
        );
    }

    private void DrawRatingFoldout(PerformanceRating rating)
    {
        int count = CountResults(rating);
        int foldoutIndex = (int)rating;

        GUIStyle style = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold
        };

        Color previousColor = GUI.contentColor;
        GUI.contentColor = GetRatingColor(rating);

        foldouts[foldoutIndex] = EditorGUILayout.Foldout(
            foldouts[foldoutIndex],
            GetRatingLabel(rating) + " (" + count + ")",
            true,
            style
        );

        GUI.contentColor = previousColor;

        if (!foldouts[foldoutIndex]) return;

        EditorGUI.indentLevel++;

        for (int i = 0; i < results.Count; i++)
        {
            MetricResult result = results[i];

            if (result.Rating == rating)
            {
                DrawMetricResult(result);
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(3f);
    }

    private void DrawMetricResult(MetricResult result)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(result.Definition.Name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Avatar", FormatValue(result.Definition, result.Value));
        EditorGUILayout.LabelField("Bewertung", GetRatingLabel(result.Rating));
        EditorGUILayout.LabelField(
            GetRatingLabel(result.Rating) + "-Bereich",
            GetRatingRange(result.Definition, result.Rating)
        );

        if (result.Rating != PerformanceRating.Excellent)
        {
            PerformanceRating betterRating =
                (PerformanceRating)((int)result.Rating - 1);
            double betterLimit = result.Definition.Limits[(int)betterRating];
            double reduction = Math.Max(0d, result.Value - betterLimit);

            EditorGUILayout.LabelField(
                "Nächstbessere Stufe",
                "Für " + GetRatingLabel(betterRating) +
                " muss der Wert auf " + FormatValue(result.Definition, betterLimit) +
                " oder weniger reduziert werden (" +
                FormatValue(result.Definition, reduction) + " weniger).",
                EditorStyles.wordWrappedMiniLabel
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void AnalyzeAvatar(bool forceRefresh)
    {
        validationMessage = null;
        avatarDescriptor = FindAvatarDescriptor(avatarRoot);

        if (avatarRoot == null)
        {
            results.Clear();
            lastResultSignature = 0;
            Repaint();
            return;
        }

        if (avatarDescriptor == null)
        {
            results.Clear();
            lastResultSignature = 0;
            validationMessage =
                "Das ausgewählte GameObject besitzt keinen VRCAvatarDescriptor.";
            Repaint();
            return;
        }

        double[] values = CollectMetricValues(avatarRoot);
        int signature = CalculateSignature(values);

        if (!forceRefresh && signature == lastResultSignature)
        {
            return;
        }

        results.Clear();
        overallRating = PerformanceRating.Excellent;

        for (int i = 0; i < Definitions.Length; i++)
        {
            PerformanceRating rating = Evaluate(Definitions[i], values[i]);

            results.Add(new MetricResult
            {
                Definition = Definitions[i],
                Value = values[i],
                Rating = rating
            });

            if (rating > overallRating)
            {
                overallRating = rating;
            }
        }

        lastResultSignature = signature;
        Repaint();
    }

    private void ClearAnalysis()
    {
        avatarRoot = null;
        avatarDescriptor = null;
        validationMessage = null;
        results.Clear();
        lastResultSignature = 0;
        overallRating = PerformanceRating.Excellent;
        Repaint();
    }

    private void OnInspectorUpdate()
    {
        if (avatarRoot == null) return;
        if (EditorApplication.timeSinceStartup < nextAutomaticAnalysis) return;

        nextAutomaticAnalysis = EditorApplication.timeSinceStartup + 1d;
        AnalyzeAvatar(false);
    }

    private void OnHierarchyChange()
    {
        AnalyzeAvatar(false);
    }

    private void OnProjectChange()
    {
        AnalyzeAvatar(false);
    }

    private static double[] CollectMetricValues(GameObject root)
    {
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        SkinnedMeshRenderer[] skinnedMeshes =
            root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        Component[] components = root.GetComponentsInChildren<Component>(true);

        long triangles = CountTriangles(meshFilters, skinnedMeshes);
        double textureMemory = CountTextureMemoryMB(renderers);
        int materialSlots = CountMaterialSlots(renderers);
        int physBones = CountComponents(components, "VRCPhysBone", "VRCPhysBoneBase");
        int physBoneTransforms = CountPhysBoneTransforms(components);
        int physBoneColliders = CountComponents(
            components,
            "VRCPhysBoneCollider",
            "VRCPhysBoneColliderBase"
        );
        int contacts =
            CountComponents(components, "VRCContactSender", "VRCContactSenderBase") +
            CountComponents(components, "VRCContactReceiver", "VRCContactReceiverBase");
        int bones = CountBones(skinnedMeshes);

        return new double[]
        {
            triangles,
            textureMemory,
            materialSlots,
            skinnedMeshes.Length,
            physBones,
            physBoneTransforms,
            physBoneColliders,
            contacts,
            bones,
            animators.Length
        };
    }

    private static long CountTriangles(
        MeshFilter[] meshFilters,
        SkinnedMeshRenderer[] skinnedMeshes)
    {
        long total = 0;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            Mesh mesh = meshFilters[i].sharedMesh;

            if (mesh != null)
            {
                total += CountMeshTriangles(mesh);
            }
        }

        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            Mesh mesh = skinnedMeshes[i].sharedMesh;

            if (mesh != null)
            {
                total += CountMeshTriangles(mesh);
            }
        }

        return total;
    }

    private static long CountMeshTriangles(Mesh mesh)
    {
        long total = 0;

        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            total += (long)mesh.GetIndexCount(subMesh) / 3L;
        }

        return total;
    }

    private static int CountMaterialSlots(Renderer[] renderers)
    {
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            count += materials != null ? materials.Length : 0;
        }

        return count;
    }

    private static double CountTextureMemoryMB(Renderer[] renderers)
    {
        HashSet<Material> materials = new HashSet<Material>();
        HashSet<Texture> textures = new HashSet<Texture>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] rendererMaterials = renderers[i].sharedMaterials;

            if (rendererMaterials == null) continue;

            for (int materialIndex = 0;
                 materialIndex < rendererMaterials.Length;
                 materialIndex++)
            {
                Material material = rendererMaterials[materialIndex];

                if (material != null)
                {
                    materials.Add(material);
                }
            }
        }

        foreach (Material material in materials)
        {
            Shader shader = material.shader;
            if (shader == null) continue;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int propertyIndex = 0;
                 propertyIndex < propertyCount;
                 propertyIndex++)
            {
                if (ShaderUtil.GetPropertyType(shader, propertyIndex) !=
                    ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    continue;
                }

                string propertyName = ShaderUtil.GetPropertyName(shader, propertyIndex);
                Texture texture = material.GetTexture(propertyName);

                if (texture != null)
                {
                    textures.Add(texture);
                }
            }
        }

        long bytes = 0;

        foreach (Texture texture in textures)
        {
            long textureBytes = Profiler.GetRuntimeMemorySizeLong(texture);

            if (textureBytes <= 0)
            {
                textureBytes = (long)texture.width * texture.height * 4L;
            }

            bytes += textureBytes;
        }

        return bytes / 1024d / 1024d;
    }

    private static int CountBones(SkinnedMeshRenderer[] skinnedMeshes)
    {
        HashSet<Transform> bones = new HashSet<Transform>();

        for (int i = 0; i < skinnedMeshes.Length; i++)
        {
            Transform[] rendererBones = skinnedMeshes[i].bones;

            if (rendererBones != null)
            {
                for (int boneIndex = 0;
                     boneIndex < rendererBones.Length;
                     boneIndex++)
                {
                    if (rendererBones[boneIndex] != null)
                    {
                        bones.Add(rendererBones[boneIndex]);
                    }
                }
            }

            if (skinnedMeshes[i].rootBone != null)
            {
                bones.Add(skinnedMeshes[i].rootBone);
            }
        }

        return bones.Count;
    }

    private static int CountPhysBoneTransforms(Component[] components)
    {
        HashSet<Transform> affectedTransforms = new HashSet<Transform>();

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (!MatchesType(component, "VRCPhysBone", "VRCPhysBoneBase"))
            {
                continue;
            }

            Transform physBoneRoot =
                GetMemberValue<Transform>(component, "rootTransform") ??
                GetMemberValue<Transform>(component, "RootTransform") ??
                component.transform;

            if (physBoneRoot == null) continue;

            HashSet<Transform> ignoredTransforms = GetTransformSet(
                component,
                "ignoreTransforms",
                "IgnoreTransforms"
            );

            Transform[] children = physBoneRoot.GetComponentsInChildren<Transform>(true);

            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                Transform child = children[childIndex];

                if (!IsIgnoredTransform(child, ignoredTransforms))
                {
                    affectedTransforms.Add(child);
                }
            }
        }

        return affectedTransforms.Count;
    }

    private static bool IsIgnoredTransform(
        Transform transform,
        HashSet<Transform> ignoredTransforms)
    {
        foreach (Transform ignored in ignoredTransforms)
        {
            if (ignored != null &&
                (transform == ignored || transform.IsChildOf(ignored)))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<Transform> GetTransformSet(
        Component component,
        params string[] memberNames)
    {
        HashSet<Transform> transforms = new HashSet<Transform>();

        for (int i = 0; i < memberNames.Length; i++)
        {
            object value = GetMemberValue(component, memberNames[i]);
            IEnumerable enumerable = value as IEnumerable;

            if (enumerable == null) continue;

            foreach (object item in enumerable)
            {
                Transform transform = item as Transform;

                if (transform != null)
                {
                    transforms.Add(transform);
                }
            }
        }

        return transforms;
    }

    private static T GetMemberValue<T>(object target, string memberName)
        where T : class
    {
        return GetMemberValue(target, memberName) as T;
    }

    private static object GetMemberValue(object target, string memberName)
    {
        if (target == null) return null;

        Type type = target.GetType();
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        while (type != null)
        {
            FieldInfo field = type.GetField(memberName, flags);

            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = type.GetProperty(memberName, flags);

            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(target, null);
            }

            type = type.BaseType;
        }

        return null;
    }

    private static int CountComponents(
        Component[] components,
        params string[] acceptedTypeNames)
    {
        int count = 0;

        for (int i = 0; i < components.Length; i++)
        {
            if (MatchesType(components[i], acceptedTypeNames))
            {
                count++;
            }
        }

        return count;
    }

    private static bool MatchesType(
        Component component,
        params string[] acceptedTypeNames)
    {
        if (component == null) return false;

        Type type = component.GetType();

        while (type != null)
        {
            for (int i = 0; i < acceptedTypeNames.Length; i++)
            {
                if (type.Name == acceptedTypeNames[i])
                {
                    return true;
                }
            }

            type = type.BaseType;
        }

        return false;
    }

    private static Component FindAvatarDescriptor(GameObject avatar)
    {
        if (avatar == null) return null;

        Component[] components = avatar.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component != null &&
                component.GetType().Name == "VRCAvatarDescriptor")
            {
                return component;
            }
        }

        return null;
    }

    private static PerformanceRating Evaluate(
        MetricDefinition definition,
        double value)
    {
        for (int i = 0; i < definition.Limits.Length; i++)
        {
            if (value <= definition.Limits[i])
            {
                return (PerformanceRating)i;
            }
        }

        return PerformanceRating.VeryPoor;
    }

    private static int CalculateSignature(double[] values)
    {
        unchecked
        {
            int signature = 17;

            for (int i = 0; i < values.Length; i++)
            {
                signature = signature * 31 + values[i].GetHashCode();
            }

            return signature;
        }
    }

    private int CountResults(PerformanceRating rating)
    {
        int count = 0;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Rating == rating)
            {
                count++;
            }
        }

        return count;
    }

    private static string GetRatingRange(
        MetricDefinition definition,
        PerformanceRating rating)
    {
        if (rating == PerformanceRating.Excellent)
        {
            return "bis " + FormatValue(definition, definition.Limits[0]);
        }

        if (rating == PerformanceRating.VeryPoor)
        {
            return "über " + FormatValue(definition, definition.Limits[3]);
        }

        int ratingIndex = (int)rating;
        double lowerBound = definition.Limits[ratingIndex - 1];

        if (definition.WholeNumber)
        {
            lowerBound += 1d;
        }

        string lowerText = definition.WholeNumber
            ? FormatValue(definition, lowerBound)
            : "über " + FormatValue(definition, lowerBound);

        return lowerText + " bis " +
               FormatValue(definition, definition.Limits[ratingIndex]);
    }

    private static string FormatValue(
        MetricDefinition definition,
        double value)
    {
        string formatted = definition.WholeNumber
            ? Math.Round(value).ToString("N0")
            : value.ToString("F1");

        return string.IsNullOrEmpty(definition.Unit)
            ? formatted
            : formatted + " " + definition.Unit;
    }

    private static string GetRatingLabel(PerformanceRating rating)
    {
        return rating == PerformanceRating.VeryPoor
            ? "Very Poor"
            : rating.ToString();
    }

    private static Color GetRatingColor(PerformanceRating rating)
    {
        switch (rating)
        {
            case PerformanceRating.VeryPoor:
                return new Color(1f, 0.35f, 0.35f);
            case PerformanceRating.Poor:
                return new Color(1f, 0.55f, 0.25f);
            case PerformanceRating.Medium:
                return new Color(1f, 0.8f, 0.25f);
            case PerformanceRating.Good:
                return new Color(0.55f, 0.85f, 1f);
            default:
                return new Color(0.45f, 1f, 0.55f);
        }
    }
}
