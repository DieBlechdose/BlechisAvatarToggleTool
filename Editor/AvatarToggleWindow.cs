using UnityEditor;
using UnityEngine;

public class AvatarToggleWindow : EditorWindow
{
    private GameObject selectedRoot;
    private string search = "";
    private Vector2 scroll;

    [MenuItem("Tools/Avatar Toggle Tool")]
    public static void Open()
    {
        GetWindow<AvatarToggleWindow>("Avatar Toggles");
    }

    private void OnGUI()
    {
        GUILayout.Label("Avatar Toggle Tool", EditorStyles.boldLabel);

        selectedRoot = (GameObject)EditorGUILayout.ObjectField(
            "Avatar Root",
            selectedRoot,
            typeof(GameObject),
            true
        );

        search = EditorGUILayout.TextField("Search", search);

        if (selectedRoot == null)
        {
            EditorGUILayout.HelpBox(
                "Drag your avatar root object here.",
                MessageType.Info
            );
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (Transform child in selectedRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!string.IsNullOrEmpty(search) &&
                !child.name.ToLower().Contains(search.ToLower()))
                continue;

            bool active = child.gameObject.activeSelf;

            bool newActive =
                EditorGUILayout.ToggleLeft(child.name, active);

            if (newActive != active)
            {
                Undo.RecordObject(child.gameObject, "Toggle Object");
                child.gameObject.SetActive(newActive);
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
