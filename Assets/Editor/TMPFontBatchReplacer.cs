using UnityEngine;
using UnityEditor;
using TMPro;

public class TMPFontBatchReplacer : EditorWindow
{
    private TMP_FontAsset newFont;

    [MenuItem("Tools/TMP/Batch Replace TMP Font")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontBatchReplacer>("TMP Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Replace TMP Font", EditorStyles.boldLabel);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "New TMP Font Asset",
            newFont,
            typeof(TMP_FontAsset),
            false
        );

        if (GUILayout.Button("Replace In Current Scene"))
        {
            ReplaceInCurrentScene();
        }

        if (GUILayout.Button("Replace In Selected Objects"))
        {
            ReplaceInSelectedObjects();
        }
    }

    private void ReplaceInCurrentScene()
    {
        if (newFont == null)
        {
            Debug.LogError("Please assign a new TMP Font Asset first.");
            return;
        }

        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        int count = 0;

        foreach (TMP_Text text in texts)
        {
            Undo.RecordObject(text, "Replace TMP Font");
            text.font = newFont;
            EditorUtility.SetDirty(text);
            count++;
        }

        Debug.Log($"Replaced TMP font on {count} TMP_Text objects in current scene.");
    }

    private void ReplaceInSelectedObjects()
    {
        if (newFont == null)
        {
            Debug.LogError("Please assign a new TMP Font Asset first.");
            return;
        }

        int count = 0;

        foreach (GameObject obj in Selection.gameObjects)
        {
            TMP_Text[] texts = obj.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                Undo.RecordObject(text, "Replace TMP Font");
                text.font = newFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        Debug.Log($"Replaced TMP font on {count} TMP_Text objects in selected objects.");
    }
}