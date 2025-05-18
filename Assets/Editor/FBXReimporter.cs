using UnityEditor;
using UnityEngine;

public class FBXReimporter
{
    [MenuItem("Assets/Reimport FBX", true)] // Validation — only shows if FBX is selected
    static bool ValidateReimport()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
    }

    [MenuItem("Assets/Reimport FBX")] // Actual menu action
    static void ReimportSelectedFBX()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Reimported FBX: {path}");
            }
        }
    }
}
