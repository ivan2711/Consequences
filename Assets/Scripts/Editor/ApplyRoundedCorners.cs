using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ApplyRoundedCorners : EditorWindow
{
    private const int TextureSize = 128;
    private const int CornerRadius = 32;
    private const string SpritePath = "Assets/Sprites/RoundedRect.png";

    private static readonly string[] ScenePaths = new string[]
    {
        "Assets/Scenes/BankScene.unity",
        "Assets/Scenes/EmergencyFund.unity",
        "Assets/Scenes/GameChoice.unity",
        "Assets/Scenes/Home.unity",
        "Assets/Scenes/Progress.unity",
        "Assets/Scenes/Settings.unity",
        "Assets/Scenes/Spending.unity"
    };

    private static readonly string[] SkipNamePatterns = new string[]
    {
        "background", "duck", "bar", "fill", "checkmark",
        "handle", "sliding", "viewport", "content", "scrollbar",
        "label", "text", "star", "icon", "mask", "shine", "beak",
        "eye", "wing", "tail", "head", "body", "message"
    };

    [MenuItem("Tools/Apply Rounded Corners to All Scenes")]
    static void Apply()
    {
        Sprite roundedSprite = GenerateAndImportSprite();
        if (roundedSprite == null)
        {
            Debug.LogError("[RoundedCorners] Failed to generate sprite.");
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string originalScene = EditorSceneManager.GetActiveScene().path;

        int totalApplied = 0;

        // Process all scenes
        foreach (string scenePath in ScenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning("[RoundedCorners] Scene not found: " + scenePath);
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath);
            int count = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ApplyRecursive(root.transform, roundedSprite, ref count);
            }

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[RoundedCorners] " + scenePath + ": Applied to " + count + " objects");
            totalApplied += count;
        }

        // Process prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            int count = 0;
            ApplyRecursive(prefab.transform, roundedSprite, ref count);

            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                Debug.Log("[RoundedCorners] Prefab " + prefabPath + ": Applied to " + count + " objects");
                totalApplied += count;
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        // Reopen original scene
        if (!string.IsNullOrEmpty(originalScene) && System.IO.File.Exists(originalScene))
            EditorSceneManager.OpenScene(originalScene);

        Debug.Log("[RoundedCorners] Done! Applied to " + totalApplied + " total objects.");
    }

    static void ApplyRecursive(Transform parent, Sprite sprite, ref int count)
    {
        if (ShouldApply(parent.gameObject))
        {
            Image img = parent.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            EditorUtility.SetDirty(img);
            count++;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            ApplyRecursive(parent.GetChild(i), sprite, ref count);
        }
    }

    static bool ShouldApply(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) return false;

        string nameLower = go.name.ToLower();

        // Buttons always get rounded corners
        if (go.GetComponent<Button>() != null) return true;

        // Skip known non-panel patterns
        foreach (string pattern in SkipNamePatterns)
        {
            if (nameLower.Contains(pattern)) return false;
        }

        // Skip filled images (progress bars)
        if (img.type == Image.Type.Filled) return false;

        // Skip fully transparent (invisible layout containers)
        if (img.color.a < 0.01f) return false;

        // Panels get rounded corners
        if (nameLower.Contains("panel")) return true;

        // UI containers with child text
        if (go.GetComponentInChildren<TMPro.TextMeshProUGUI>() != null) return true;

        return false;
    }

    // ==================== SPRITE GENERATION ====================

    static Sprite GenerateAndImportSprite()
    {
        // Create Sprites folder if needed
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        // Generate texture
        Texture2D tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float alpha = 1f;

                bool inCornerX = (x < CornerRadius) || (x >= TextureSize - CornerRadius);
                bool inCornerY = (y < CornerRadius) || (y >= TextureSize - CornerRadius);

                if (inCornerX && inCornerY)
                {
                    float cx = (x < CornerRadius) ? CornerRadius - 0.5f : TextureSize - CornerRadius - 0.5f;
                    float cy = (y < CornerRadius) ? CornerRadius - 0.5f : TextureSize - CornerRadius - 0.5f;

                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    alpha = Mathf.Clamp01(CornerRadius - dist);
                }

                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Save PNG
        byte[] pngData = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(SpritePath, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

        // Configure import settings
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spriteBorder = new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius);
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
