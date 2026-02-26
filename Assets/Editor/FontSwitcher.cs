using UnityEngine;
using UnityEditor;
using TMPro;

public class FontSwitcher : EditorWindow
{
    public TMP_FontAsset newFont;

    [MenuItem("Tools/Replace All TMP Fonts")]
    public static void ShowWindow()
    {
        GetWindow<FontSwitcher>("Font Switcher");
    }

    void OnGUI()
    {
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Target Font", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace All in Current Scene"))
        {
            if (newFont == null) return;

            // Finds both UI and 3D TextMeshPro components
            TMP_Text[] allText = FindObjectsOfType<TMP_Text>(true); 
            
            foreach (TMP_Text text in allText)
            {
                Undo.RecordObject(text, "Replace Font");
                text.font = newFont;
                EditorUtility.SetDirty(text);
            }
            Debug.Log($"Successfully replaced font on {allText.Length} objects.");
        }
    }
}