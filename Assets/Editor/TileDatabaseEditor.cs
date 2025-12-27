using UnityEngine;
using UnityEditor;

namespace DorkyProductions
{
    
[CustomEditor(typeof(TileDatabase))]
public class TileDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default script field
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((TileDatabase)target), typeof(TileDatabase), false);
        GUI.enabled = true;

        TileDatabase db = (TileDatabase)target;

        // Draw Default list of tiles (so you can add/remove them)
        SerializedProperty listProp = serializedObject.FindProperty("allTileDefinitions");
        EditorGUILayout.PropertyField(listProp, true);
        
        // Draw Default list of skills (so you can add/remove them)
        SerializedProperty skillsProp = serializedObject.FindProperty("allSkills");
        EditorGUILayout.PropertyField(skillsProp, true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Probability Balance", EditorStyles.boldLabel);
        
        // Draw the Balance Table
        if (db.allTileDefinitions != null)
        {
            float totalWeight = 0;
            foreach (var t in db.allTileDefinitions) 
                if(t != null) totalWeight += t.spawnWeight;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tile", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Weight", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Chance %", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Visual", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            foreach (var tileDef in db.allTileDefinitions)
            {
                if (tileDef == null) continue;

                EditorGUILayout.BeginHorizontal();
                
                // Name
                EditorGUILayout.LabelField(tileDef.name, GUILayout.Width(80));

                // Editable Weight
                EditorGUI.BeginChangeCheck();
                int newWeight = EditorGUILayout.IntField(tileDef.spawnWeight, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tileDef, "Change Tile Weight");
                    tileDef.spawnWeight = newWeight;
                    db.CalculateProbabilities(); // Recalculate immediately
                    EditorUtility.SetDirty(tileDef);
                }

                // Percent String
                float percent = (totalWeight > 0) ? (tileDef.spawnWeight / totalWeight) : 0;
                EditorGUILayout.LabelField($"{percent:P1}", GUILayout.Width(60)); // P1 formats to 12.5%

                // Visual Bar
                Rect r = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(r, percent, "");

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.HelpBox($"Total Weight: {totalWeight}", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
}