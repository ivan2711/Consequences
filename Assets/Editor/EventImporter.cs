using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// One-click importer: reads all .json files from a temp folder and copies them to Resources/Events.
/// Usage: 
///   1. Place JSON files in the project root's "TempEvents" folder
///   2. Click Tools > Import Event JSONs
///   3. Files get copied to Assets/Resources/Events/
///   4. Delete TempEvents folder when done
/// </summary>
public class EventImporter
{
    [MenuItem("Tools/Import Event JSONs")]
    public static void ImportEvents()
    {
        // Source: TempEvents folder at project root (sibling to Assets)
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourceDir = Path.Combine(projectRoot, "TempEvents");

        // Target: Assets/Resources/Events
        string targetDir = Path.Combine(Application.dataPath, "Resources", "Events");

        if (!Directory.Exists(sourceDir))
        {
            // Also try inside Assets for convenience
            sourceDir = Path.Combine(Application.dataPath, "TempEvents");
        }

        if (!Directory.Exists(sourceDir))
        {
            Debug.LogError($"[EventImporter] No source folder found. Place JSON files in:\n" +
                          $"  {Path.Combine(projectRoot, "TempEvents")}\n" +
                          $"  or {Path.Combine(Application.dataPath, "TempEvents")}");
            return;
        }

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        string[] jsonFiles = Directory.GetFiles(sourceDir, "*.json");
        if (jsonFiles.Length == 0)
        {
            Debug.LogWarning("[EventImporter] No .json files found in " + sourceDir);
            return;
        }

        int count = 0;
        int skipped = 0;
        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);
            string destPath = Path.Combine(targetDir, fileName);

            // Quick validation: try parsing
            string json = File.ReadAllText(file);
            EmergencyFundEvent evt = JsonUtility.FromJson<EmergencyFundEvent>(json);
            if (evt == null || string.IsNullOrEmpty(evt.id))
            {
                Debug.LogWarning($"[EventImporter] Skipped invalid file: {fileName}");
                skipped++;
                continue;
            }

            File.Copy(file, destPath, overwrite: true);
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[EventImporter] Imported {count} event files ({skipped} skipped) to Resources/Events");
    }

    [MenuItem("Tools/Validate Event JSONs")]
    public static void ValidateEvents()
    {
        string targetDir = Path.Combine(Application.dataPath, "Resources", "Events");
        if (!Directory.Exists(targetDir))
        {
            Debug.LogError("[EventImporter] No Resources/Events folder found");
            return;
        }

        string[] files = Directory.GetFiles(targetDir, "*.json");
        int valid = 0;
        int errors = 0;

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string json = File.ReadAllText(file);

            try
            {
                EmergencyFundEvent evt = JsonUtility.FromJson<EmergencyFundEvent>(json);
                if (string.IsNullOrEmpty(evt.id) || string.IsNullOrEmpty(evt.type))
                {
                    Debug.LogError($"  [FAIL] {fileName}: missing id or type");
                    errors++;
                    continue;
                }
                if (evt.choices == null || evt.choices.Length == 0)
                {
                    Debug.LogError($"  [FAIL] {fileName}: no choices");
                    errors++;
                    continue;
                }
                if (evt.currencyCode != "GBP")
                {
                    Debug.LogError($"  [FAIL] {fileName}: currency is {evt.currencyCode}, expected GBP");
                    errors++;
                    continue;
                }
                valid++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"  [FAIL] {fileName}: {e.Message}");
                errors++;
            }
        }

        Debug.Log($"[EventImporter] Validation: {valid} valid, {errors} errors, {files.Length} total");
    }
}
