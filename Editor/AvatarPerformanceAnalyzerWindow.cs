using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class AvatarPerformanceAnalyzerWindow : EditorWindow
{
    private enum TargetPlatform { PC, Mobile }
    private enum PerformanceRating { Excellent, Good, Medium, Poor, VeryPoor }

    private sealed class MetricDefinition
    {
        public readonly string Name;
        public readonly string ValuePath;
        public readonly double[] PcLimits;
        public readonly double[] MobileLimits;
        public readonly string Unit;
        public readonly bool DecimalValue;
        public readonly bool BooleanValue;
        public readonly Vector3[] BoundsLimits;

        public MetricDefinition(
            string name,
            string valuePath,
            double[] pcLimits,
            double[] mobileLimits,
            string unit = "",
            bool decimalValue = false,
            bool booleanValue = false)
        {
            Name = name;
            ValuePath = valuePath;
            PcLimits = pcLimits;
            MobileLimits = mobileLimits;
            Unit = unit;
            DecimalValue = decimalValue;
            BooleanValue = booleanValue;
        }

        public MetricDefinition(string name, string valuePath, Vector3[] boundsLimits)
        {
            Name = name;
            ValuePath = valuePath;
            BoundsLimits = boundsLimits;
            PcLimits = new double[0];
            MobileLimits = new double[0];
            Unit = "m";
        }

        public bool IsBounds { get { return BoundsLimits != null; } }

        public double[] GetLimits(TargetPlatform platform)
        {
            return platform == TargetPlatform.Mobile ? MobileLimits : PcLimits;
        }

        public bool Supports(TargetPlatform platform)
        {
            return IsBounds || GetLimits(platform) != null;
        }
    }

    private sealed class MetricResult
    {
        public MetricDefinition Definition;
        public object Value;
        public PerformanceRating? Rating;
    }

    private static double[] L(double excellent, double good, double medium, double poor)
    {
        return new[] { excellent, good, medium, poor };
    }

    private static readonly Vector3[] BoundsLimits =
    {
        new Vector3(2.5f, 2.5f, 2.5f),
        new Vector3(4f, 4f, 4f),
        new Vector3(5f, 6f, 5f),
        new Vector3(5f, 6f, 5f)
    };

    // Values and limits mirror the official VRChat Performance Ranks tables.
    // https://creators.vrchat.com/avatars/avatar-performance-ranking-system/
    // A null platform limit means that VRChat does not rank that category there.
    private static readonly MetricDefinition[] Definitions =
    {
        new MetricDefinition("Triangles", "polyCount", L(32000, 70000, 70000, 70000), L(7500, 10000, 15000, 20000)),
        new MetricDefinition("Bounds Size", "aabb", BoundsLimits),
        new MetricDefinition("Texture Memory", "textureMegabytes", L(40, 75, 110, 150), L(10, 18, 25, 40), "MB", true),
        new MetricDefinition("Skinned Meshes", "skinnedMeshCount", L(1, 2, 8, 16), L(1, 1, 2, 2)),
        new MetricDefinition("Basic Meshes", "meshCount", L(4, 8, 16, 24), L(1, 1, 2, 2)),
        new MetricDefinition("Material Slots", "materialCount", L(4, 8, 16, 32), L(1, 1, 2, 4)),
        new MetricDefinition("PhysBones Components", "physBone.componentCount", L(4, 8, 16, 32), L(0, 4, 6, 8)),
        new MetricDefinition("PhysBones Affected Transforms", "physBone.transformCount", L(16, 64, 128, 256), L(0, 16, 32, 64)),
        new MetricDefinition("PhysBones Colliders", "physBone.colliderCount", L(4, 8, 16, 32), L(0, 4, 8, 16)),
        new MetricDefinition("PhysBones Collision Check Count", "physBone.collisionCheckCount", L(32, 128, 256, 512), L(0, 16, 32, 64)),
        new MetricDefinition("Contacts", "contactCount", L(8, 16, 24, 32), L(2, 4, 8, 16)),
        new MetricDefinition("Constraint Count", "constraintsCount", L(100, 250, 300, 350), L(30, 60, 120, 150)),
        new MetricDefinition("Constraint Depth", "constraintDepth", L(20, 50, 80, 100), L(5, 15, 35, 50)),
        new MetricDefinition("Animators", "animatorCount", L(1, 4, 16, 32), L(1, 1, 1, 2)),
        new MetricDefinition("Bones", "boneCount", L(75, 150, 256, 400), L(75, 90, 150, 150)),
        new MetricDefinition("Lights", "lightCount", L(0, 0, 0, 1), null),
        new MetricDefinition("Particle Systems", "particleSystemCount", L(0, 4, 8, 16), L(0, 0, 0, 2)),
        new MetricDefinition("Total Particles Active", "particleTotalCount", L(0, 300, 1000, 2500), L(0, 0, 0, 200)),
        new MetricDefinition("Mesh Particle Active Polys", "particleMaxMeshPolyCount", L(0, 1000, 2000, 5000), L(0, 0, 0, 400)),
        new MetricDefinition("Particle Trails Enabled", "particleTrailsEnabled", L(0, 0, 1, 1), L(0, 0, 0, 1), booleanValue: true),
        new MetricDefinition("Particle Collision Enabled", "particleCollisionEnabled", L(0, 0, 1, 1), L(0, 0, 0, 1), booleanValue: true),
        new MetricDefinition("Trail Renderers", "trailRendererCount", L(1, 2, 4, 8), L(0, 0, 0, 1)),
        new MetricDefinition("Line Renderers", "lineRendererCount", L(1, 2, 4, 8), L(0, 0, 0, 1)),
        new MetricDefinition("Raycasts", "raycastCount", L(1, 4, 8, 15), L(1, 2, 4, 8)),
        new MetricDefinition("Cloths", "clothCount", L(0, 1, 1, 1), null),
        new MetricDefinition("Total Cloth Vertices", "clothMaxVertices", L(0, 50, 100, 200), null),
        new MetricDefinition("Physics Colliders", "physicsColliderCount", L(0, 1, 8, 8), null),
        new MetricDefinition("Physics Rigidbodies", "physicsRigidbodyCount", L(0, 1, 8, 8), null),
        new MetricDefinition("Audio Sources", "audioSourceCount", L(1, 4, 8, 8), null)
    };

    private readonly List<MetricResult> results = new List<MetricResult>();
    private readonly bool[] foldouts = { true, true, true, true, true };
    private bool unavailableFoldout = true;
    private GameObject avatarRoot;
    private Vector2 scrollPosition;
    private TargetPlatform targetPlatform = TargetPlatform.PC;
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
        minSize = new Vector2(520f, 520f);
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawAvatarSelection();
        DrawPlatformSelection();
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
                MessageType.Info);
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
        DrawUnavailableFoldout();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        GUILayout.Label("Avatar Performance Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Alle Werte und Grenzstufen entsprechen der offiziellen VRChat Performance-Ranks-Tabelle.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6f);
    }

    private void DrawAvatarSelection()
    {
        EditorGUI.BeginChangeCheck();
        GameObject selected = (GameObject)EditorGUILayout.ObjectField(
            "Avatar", avatarRoot, typeof(GameObject), true);

        if (EditorGUI.EndChangeCheck())
        {
            avatarRoot = selected;
            AnalyzeAvatar(true);
        }
    }

    private void DrawPlatformSelection()
    {
        EditorGUI.BeginChangeCheck();
        TargetPlatform selected = (TargetPlatform)EditorGUILayout.EnumPopup(
            "VRChat Platform", targetPlatform);

        if (EditorGUI.EndChangeCheck())
        {
            targetPlatform = selected;
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
        MessageType type = overallRating >= PerformanceRating.Poor
            ? MessageType.Error
            : overallRating == PerformanceRating.Medium
                ? MessageType.Warning
                : MessageType.Info;

        EditorGUILayout.HelpBox(
            "Gesamtbewertung (" + GetPlatformLabel() + "): " + GetRatingLabel(overallRating),
            type);
    }

    private void DrawRatingFoldout(PerformanceRating rating)
    {
        int count = CountResults(rating);
        int index = (int)rating;
        GUIStyle style = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
        Color oldColor = GUI.contentColor;
        GUI.contentColor = GetRatingColor(rating);

        foldouts[index] = EditorGUILayout.Foldout(
            foldouts[index],
            GetRatingLabel(rating) + " (" + count + ")",
            true,
            style);

        GUI.contentColor = oldColor;
        if (!foldouts[index]) return;

        EditorGUI.indentLevel++;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Rating == rating)
            {
                DrawMetricResult(results[i]);
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(3f);
    }

    private void DrawUnavailableFoldout()
    {
        int count = CountUnavailable();
        if (count == 0) return;

        unavailableFoldout = EditorGUILayout.Foldout(
            unavailableFoldout,
            "Nicht vom installierten VRCSDK geliefert (" + count + ")",
            true);

        if (!unavailableFoldout) return;

        EditorGUI.indentLevel++;

        for (int i = 0; i < results.Count; i++)
        {
            if (!results[i].Rating.HasValue)
            {
                DrawMetricResult(results[i]);
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawMetricResult(MetricResult result)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(result.Definition.Name, EditorStyles.boldLabel);

        if (!result.Rating.HasValue)
        {
            EditorGUILayout.LabelField("Avatar", "Nicht verfügbar");
            EditorGUILayout.LabelField(
                "Hinweis",
                "Diese Kennzahl wird von der installierten VRCSDK-Version nicht bereitgestellt.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField("Avatar", FormatValue(result.Definition, result.Value));
        EditorGUILayout.LabelField("Bewertung", GetRatingLabel(result.Rating.Value));
        EditorGUILayout.LabelField(
            GetRatingLabel(result.Rating.Value) + "-Maximum",
            FormatRatingLimit(result.Definition, result.Rating.Value));

        if (result.Rating.Value != PerformanceRating.Excellent)
        {
            PerformanceRating better = (PerformanceRating)((int)result.Rating.Value - 1);
            EditorGUILayout.LabelField(
                "Nächstbessere Stufe",
                GetImprovementText(result, better),
                EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void AnalyzeAvatar(bool forceRefresh)
    {
        validationMessage = null;

        if (avatarRoot == null)
        {
            results.Clear();
            lastResultSignature = 0;
            Repaint();
            return;
        }

        if (FindAvatarDescriptor(avatarRoot) == null)
        {
            results.Clear();
            lastResultSignature = 0;
            validationMessage = "Das ausgewählte GameObject besitzt keinen VRCAvatarDescriptor.";
            Repaint();
            return;
        }

        object snapshot;
        string sdkError;

        if (!TryCollectSdkStats(avatarRoot, targetPlatform == TargetPlatform.Mobile, out snapshot, out sdkError))
        {
            results.Clear();
            validationMessage = sdkError;
            Repaint();
            return;
        }

        List<MetricResult> collected = BuildResults(snapshot);
        int signature = CalculateSignature(collected);

        if (!forceRefresh && signature == lastResultSignature)
        {
            return;
        }

        results.Clear();
        results.AddRange(collected);
        overallRating = PerformanceRating.Excellent;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Rating.HasValue && results[i].Rating.Value > overallRating)
            {
                overallRating = results[i].Rating.Value;
            }
        }

        lastResultSignature = signature;
        Repaint();
    }

    private List<MetricResult> BuildResults(object snapshot)
    {
        List<MetricResult> collected = new List<MetricResult>();

        for (int i = 0; i < Definitions.Length; i++)
        {
            MetricDefinition definition = Definitions[i];
            if (!definition.Supports(targetPlatform)) continue;

            object value;
            PerformanceRating? rating = null;

            if (TryGetPathValue(snapshot, definition.ValuePath, out value))
            {
                rating = Evaluate(definition, value);
            }

            collected.Add(new MetricResult
            {
                Definition = definition,
                Value = value,
                Rating = rating
            });
        }

        return collected;
    }

    private static bool TryCollectSdkStats(
        GameObject root,
        bool mobile,
        out object snapshot,
        out string error)
    {
        snapshot = null;
        error = null;

        try
        {
            Type statsType = FindType(
                "VRC.SDKBase.Validation.Performance.Stats.AvatarPerformanceStats");
            Type analyzerType = FindType(
                "VRC.SDKBase.Validation.Performance.AvatarPerformance");

            if (statsType == null || analyzerType == null)
            {
                error = "Die VRCSDK-Performance-Analyse wurde nicht gefunden. Bitte aktualisiere das VRChat SDK.";
                return false;
            }

            object stats = Activator.CreateInstance(statsType, new object[] { mobile });
            MethodInfo calculate = analyzerType.GetMethod(
                "CalculatePerformanceStats",
                BindingFlags.Public | BindingFlags.Static);
            MethodInfo getSnapshot = statsType.GetMethod(
                "GetSnapshot",
                BindingFlags.Public | BindingFlags.Instance);

            if (calculate == null || getSnapshot == null)
            {
                error = "Die installierte VRCSDK-Version stellt die benötigte Performance-API nicht bereit.";
                return false;
            }

            calculate.Invoke(null, new object[] { root.name, root, stats, mobile });
            snapshot = getSnapshot.Invoke(stats, null);
            return snapshot != null;
        }
        catch (TargetInvocationException exception)
        {
            Exception cause = exception.InnerException ?? exception;
            error = "VRCSDK-Analyse fehlgeschlagen: " + cause.Message;
            return false;
        }
        catch (Exception exception)
        {
            error = "VRCSDK-Analyse fehlgeschlagen: " + exception.Message;
            return false;
        }
    }

    private static Type FindType(string fullName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(fullName, false);
            if (type != null) return type;
        }

        return null;
    }

    private static bool TryGetPathValue(object source, string path, out object value)
    {
        value = source;
        string[] segments = path.Split('.');

        for (int i = 0; i < segments.Length; i++)
        {
            if (value == null) return false;
            value = GetMemberValue(value, segments[i]);
        }

        return value != null;
    }

    private static object GetMemberValue(object source, string memberName)
    {
        if (source == null) return null;

        Type type = source.GetType();
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        FieldInfo field = type.GetField(memberName, flags);
        if (field != null) return field.GetValue(source);

        PropertyInfo property = type.GetProperty(memberName, flags);
        return property != null && property.GetIndexParameters().Length == 0
            ? property.GetValue(source, null)
            : null;
    }

    private PerformanceRating Evaluate(MetricDefinition definition, object value)
    {
        if (definition.IsBounds)
        {
            Bounds bounds = (Bounds)value;
            Vector3 size = bounds.size;

            for (int i = 0; i < definition.BoundsLimits.Length; i++)
            {
                Vector3 limit = definition.BoundsLimits[i];

                if (size.x <= limit.x && size.y <= limit.y && size.z <= limit.z)
                {
                    return (PerformanceRating)i;
                }
            }

            return PerformanceRating.VeryPoor;
        }

        double numericValue = definition.BooleanValue
            ? ((bool)value ? 1d : 0d)
            : Convert.ToDouble(value);
        double[] limits = definition.GetLimits(targetPlatform);
        for (int i = 0; i < limits.Length; i++)
        {
            if (numericValue <= limits[i])
            {
                return (PerformanceRating)i;
            }
        }

        return PerformanceRating.VeryPoor;
    }


    private string GetImprovementText(MetricResult result, PerformanceRating better)
    {
        MetricDefinition definition = result.Definition;

        if (definition.IsBounds)
        {
            return "Für " + GetRatingLabel(better) + " darf die Bounds Size höchstens " +
                   FormatBounds(definition.BoundsLimits[(int)better]) + " betragen.";
        }

        double limit = definition.GetLimits(targetPlatform)[(int)better];

        if (definition.BooleanValue)
        {
            return "Für " + GetRatingLabel(better) + " muss der Wert " +
                   (limit > 0d ? "True" : "False") + " sein.";
        }

        double value = Convert.ToDouble(result.Value);
        double reduction = Math.Max(0d, value - limit);

        return "Für " + GetRatingLabel(better) + " muss der Wert auf " +
               FormatNumber(definition, limit) + " oder weniger reduziert werden (" +
               FormatNumber(definition, reduction) + " weniger).";
    }

    private string FormatRatingLimit(MetricDefinition definition, PerformanceRating rating)
    {
        if (rating == PerformanceRating.VeryPoor)
        {
            if (definition.IsBounds)
            {
                return "über " + FormatBounds(definition.BoundsLimits[3]);
            }

            return "über " + FormatNumber(
                definition,
                definition.GetLimits(targetPlatform)[3]);
        }

        if (definition.IsBounds)
        {
            return FormatBounds(definition.BoundsLimits[(int)rating]);
        }

        return FormatNumber(
            definition,
            definition.GetLimits(targetPlatform)[(int)rating]);
    }

    private static string FormatValue(MetricDefinition definition, object value)
    {
        if (definition.IsBounds)
        {
            return FormatBounds(((Bounds)value).size);
        }

        if (definition.BooleanValue)
        {
            return (bool)value ? "True" : "False";
        }

        return FormatNumber(definition, Convert.ToDouble(value));
    }

    private static string FormatNumber(MetricDefinition definition, double value)
    {
        string text = definition.DecimalValue
            ? value.ToString("F1")
            : Math.Round(value).ToString("N0");

        return string.IsNullOrEmpty(definition.Unit)
            ? text
            : text + " " + definition.Unit;
    }

    private static string FormatBounds(Vector3 size)
    {
        return size.x.ToString("0.##") + " × " +
               size.y.ToString("0.##") + " × " +
               size.z.ToString("0.##") + " m";
    }

    private int CalculateSignature(List<MetricResult> collected)
    {
        unchecked
        {
            int signature = 17;
            signature = signature * 31 + targetPlatform.GetHashCode();

            for (int i = 0; i < collected.Count; i++)
            {
                signature = signature * 31 + collected[i].Definition.Name.GetHashCode();
                signature = signature * 31 +
                    (collected[i].Value == null ? 0 : collected[i].Value.GetHashCode());
            }

            return signature;
        }
    }

    private int CountResults(PerformanceRating rating)
    {
        int count = 0;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Rating == rating) count++;
        }

        return count;
    }

    private int CountUnavailable()
    {
        int count = 0;

        for (int i = 0; i < results.Count; i++)
        {
            if (!results[i].Rating.HasValue) count++;
        }

        return count;
    }

    private void ClearAnalysis()
    {
        avatarRoot = null;
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

        nextAutomaticAnalysis = EditorApplication.timeSinceStartup + 2d;
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

    private static Component FindAvatarDescriptor(GameObject avatar)
    {
        if (avatar == null) return null;
        Component[] components = avatar.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null &&
                components[i].GetType().Name == "VRCAvatarDescriptor")
            {
                return components[i];
            }
        }

        return null;
    }

    private string GetPlatformLabel()
    {
        return targetPlatform == TargetPlatform.Mobile ? "Mobile / Quest" : "PC";
    }

    private static string GetRatingLabel(PerformanceRating rating)
    {
        return rating == PerformanceRating.VeryPoor ? "Very Poor" : rating.ToString();
    }

    private static Color GetRatingColor(PerformanceRating rating)
    {
        switch (rating)
        {
            case PerformanceRating.VeryPoor: return new Color(1f, 0.35f, 0.35f);
            case PerformanceRating.Poor: return new Color(1f, 0.55f, 0.25f);
            case PerformanceRating.Medium: return new Color(1f, 0.8f, 0.25f);
            case PerformanceRating.Good: return new Color(0.55f, 0.85f, 1f);
            default: return new Color(0.45f, 1f, 0.55f);
        }
    }
}

