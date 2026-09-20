using UnityEditor;
using UnityEngine;

// Jedes Blechi-Package hat eine eigene interne Kopie dieser Klasse,
// damit die Packages unabhängig voneinander installiert werden können.
// Die Sprache wird über denselben EditorPrefs-Key geteilt.
internal enum BlechiLanguage
{
    Deutsch,
    English
}

internal static class BlechiLocalization
{
    private const string LanguageKey = "BlechiAvatarTools.Language";
    private const double RefreshInterval = 0.5;

    private static readonly string[] LanguageOptions =
    {
        "Deutsch",
        "English"
    };

    private static BlechiLanguage cachedLanguage = ReadLanguage();
    private static double nextRefresh;

    private static BlechiLanguage ReadLanguage()
    {
        return (BlechiLanguage)Mathf.Clamp(EditorPrefs.GetInt(LanguageKey, 0), 0, 1);
    }

    public static BlechiLanguage Language
    {
        get
        {
            // Regelmäßig neu lesen, damit eine Änderung in einem anderen
            // Blechi-Package auch hier ankommt.
            double now = EditorApplication.timeSinceStartup;
            if (now >= nextRefresh)
            {
                cachedLanguage = ReadLanguage();
                nextRefresh = now + RefreshInterval;
            }

            return cachedLanguage;
        }
        set
        {
            if (Language == value) return;

            cachedLanguage = value;
            EditorPrefs.SetInt(LanguageKey, (int)value);
            EditorApplication.RepaintHierarchyWindow();

            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                {
                    windows[i].Repaint();
                }
            }
        }
    }

    public static string[] Options => LanguageOptions;
    public static bool IsGerman => Language == BlechiLanguage.Deutsch;

    public static string T(string german, string english)
    {
        return IsGerman ? german : english;
    }

    public static void DrawLanguagePopup()
    {
        EditorGUI.BeginChangeCheck();
        int selectedLanguage = EditorGUILayout.Popup(
            T("Sprache", "Language"),
            (int)Language,
            LanguageOptions);

        if (EditorGUI.EndChangeCheck())
        {
            Language = (BlechiLanguage)selectedLanguage;
        }
    }
}
