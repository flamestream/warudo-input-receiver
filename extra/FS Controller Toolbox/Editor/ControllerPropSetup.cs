using UnityEditor;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlameStream
{
    public static class ControllerPropSetup
    {
        [MenuItem("FS Toolbox/Setup Controller Prop", priority = 2000)]
        public static async void SetupControllerProp()
        {
            // Get the selected object in the project pane
            UnityEngine.Object selectedObject = Selection.activeObject;

            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a prefab named 'Prop.prefab' in the Project pane.", "OK");
                return;
            }

            // Check if it's a prefab and specifically named "Prop.prefab"
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            string fileName = Path.GetFileName(assetPath);

            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
            {
                EditorUtility.DisplayDialog("Invalid Selection", "Please select a prefab (.prefab file) in the Project pane.", "OK");
                return;
            }

            if (fileName != "Prop.prefab")
            {
                EditorUtility.DisplayDialog("Invalid Prefab Name", "Please select a prefab named 'Prop.prefab'. The selected prefab is named '" + fileName + "'.", "OK");
                return;
            }

            // Load the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to load the selected prefab.", "OK");
                return;
            }

            // Get the directory info
            string prefabDirectory = Path.GetDirectoryName(assetPath);

            // Check if the specific AnimSettingsReader component already exists
            Component[] allComponents = prefab.GetComponents<Component>();
            Component existingSettingsLoader = null;
            foreach (var component in allComponents)
            {
                if (component != null && component.GetType().Name == "AnimSettingsReader")
                {
                    existingSettingsLoader = component;
                    break;
                }
            }

            try
            {
                // Create unique namespace based on folder hierarchy
                string namespaceString = GenerateNamespace(prefabDirectory);
                string targetScriptPath = Path.Combine(prefabDirectory, "AnimSettingsReader.cs").Replace('\\', '/');

                Component targetComponent = existingSettingsLoader;
                bool componentWasAdded = false;

                // If no existing component, create the script and component
                if (existingSettingsLoader == null)
                {
                    // Check if custom script exists - if not, create it first
                    if (!File.Exists(targetScriptPath))
                    {
                        string templatePath = "Assets/FS Controller Toolbox/EditorSupport/AnimSettingsReader.template.txt";
                        if (!File.Exists(templatePath))
                        {
                            EditorUtility.DisplayDialog("Template Missing",
                                $"Template file not found at '{templatePath}'. Please ensure the template file exists.", "OK");
                            return;
                        }

                        // Copy the template file and replace the namespace
                        string scriptContent = File.ReadAllText(templatePath);
                        scriptContent = scriptContent.Replace("namespace Flamestream", $"namespace {namespaceString}");

                        File.WriteAllText(targetScriptPath, scriptContent);
                        AssetDatabase.ImportAsset(targetScriptPath);
                        AssetDatabase.Refresh();

                        // wait for 1 second to allow compilation
                        await System.Threading.Tasks.Task.Delay(1000);

                        EditorUtility.DisplayDialog("Script Created",
                            $"Custom script 'AnimSettingsReader.cs' has been created at '{targetScriptPath}' with namespace '{namespaceString}'. " +
                            "Please run this command again after it has compiled to proceed to the next steps of the setup.", "OK");
                        return;
                    }

                    // Script exists, now try to add the component
                    System.Type componentType = FindComponentType(namespaceString);
                    if (componentType != null)
                    {
                        targetComponent = prefab.AddComponent(componentType);
                        componentWasAdded = true;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Component Error",
                            $"Script 'AnimSettingsReader.cs' exists but the class could not be found in namespace '{namespaceString}'. Please check for compilation errors in the Console.", "OK");
                        return;
                    }
                }

                // Create/update JSON file based on Animator layer names
                string jsonFilePath = CreateAnimatorSettingsJson(prefab, prefabDirectory, namespaceString);

                // Link the JSON file to the component
                if (!string.IsNullOrEmpty(jsonFilePath) && targetComponent != null)
                {
                    LinkJsonToComponent(targetComponent, jsonFilePath);
                }

                // Mark the prefab as dirty to save changes
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();

                // Display appropriate success message
                string successMessage;
                if (componentWasAdded)
                {
                    successMessage = $"Successfully added AnimSettingsReader component to '{prefab.name}' in namespace '{namespaceString}'.";
                }
                else
                {
                    successMessage = $"Updated existing {targetComponent.GetType().Name} component on '{prefab.name}'.";
                }

                if (!string.IsNullOrEmpty(jsonFilePath))
                {
                    successMessage += $"\nJSON settings file created/updated: {jsonFilePath}";
                }

                EditorUtility.DisplayDialog("Success", successMessage, "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to setup controller prop: {ex.Message}", "OK");
            }
        }

        private static string GenerateNamespace(string prefabDirectory)
        {
            if (string.IsNullOrEmpty(prefabDirectory))
                return "Flamestream";

            // Get relative path from Assets folder
            string relativePath = prefabDirectory;
            if (relativePath.StartsWith("Assets/") || relativePath.StartsWith("Assets\\"))
            {
                relativePath = relativePath.Substring(7); // Remove "Assets/" or "Assets\"
            }

            if (string.IsNullOrEmpty(relativePath))
                return "Flamestream";

            // First replace path separators with dots, then replace other invalid characters with underscores
            string sanitized = relativePath.Replace("/", ".").Replace("\\", ".");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9_.]", "_");

            // Clean up multiple consecutive dots && underscores
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[.]+", ".");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"_+", "_");

            // Ensure each segment doesn't start with a number using regex
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"(^|\.)(\d)", "$1_$2");

            // Remove leading/trailing dots
            sanitized = sanitized.Trim('.');

            if (string.IsNullOrEmpty(sanitized))
                return "Flamestream";

            return $"Flamestream.{sanitized}";
        }

        private static System.Type FindComponentType(string fullNamespace)
        {
            string fullTypeName = $"{fullNamespace}.AnimSettingsReader";
            return FindTypeInAssemblies(fullTypeName);
        }

        private static System.Type FindTypeInAssemblies(string typeName)
        {
            // First try the simple approach
            System.Type componentType = System.Type.GetType(typeName);
            if (componentType != null)
                return componentType;

            // Search through all loaded assemblies
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                componentType = assembly.GetType(typeName);
                if (componentType != null)
                    return componentType;
            }

            return null;
        }

        private static string CreateAnimatorSettingsJson(GameObject prefab, string prefabDirectory, string namespaceString)
        {
            // Create JSON file path
            string jsonFileName = "anim-settings.json";
            string jsonFilePath = Path.Combine(prefabDirectory, jsonFileName).Replace('\\', '/');

            // Only create the file if it doesn't exist
            if (!File.Exists(jsonFilePath))
            {
                // Initialize with version and empty data structure
                var jsonObject = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["version"] = 1,
                    ["data"] = new System.Collections.Generic.Dictionary<string, object>()
                };

                // Write JSON file using stream-based helper
                WriteJsonToFile(jsonFilePath, jsonObject);
                AssetDatabase.ImportAsset(jsonFilePath);
                AssetDatabase.Refresh();
            }

            return jsonFilePath;
        }

        private static void LinkJsonToComponent(Component component, string jsonFilePath)
        {
            // Use reflection to find and set the jsonFile field
            var field = component.GetType().GetField("jsonFile");
            if (field != null && field.FieldType == typeof(TextAsset))
            {
                TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonFilePath);
                if (jsonAsset != null)
                {
                    field.SetValue(component, jsonAsset);
                }
            }
        }

        private static System.Collections.Generic.Dictionary<string, object> CreateDefaultLayerSettings()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                ["transitionDown"] = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["time"] = 0.05f,
                    ["easing"] = "Linear",
                    ["delay"] = 0
                },
                ["transitionUp"] = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["time"] = 0.05f,
                    ["easing"] = "Linear",
                    ["delay"] = 0
                },
            };
        }

        private static void EnsureLayerHasAllKeys(System.Collections.Generic.Dictionary<string, object> layerDict)
        {
            var defaultSettings = CreateDefaultLayerSettings();

            foreach (var kvp in defaultSettings)
            {
                if (!layerDict.ContainsKey(kvp.Key))
                {
                    layerDict[kvp.Key] = kvp.Value;
                }
                else if (kvp.Value is System.Collections.Generic.Dictionary<string, object> defaultSubDict)
                {
                    // Handle both Dictionary<string, object> and JObject cases
                    System.Collections.Generic.Dictionary<string, object> existingSubDict = null;

                    if (layerDict[kvp.Key] is System.Collections.Generic.Dictionary<string, object> directDict)
                    {
                        existingSubDict = directDict;
                    }
                    else if (layerDict[kvp.Key] is JObject jObj)
                    {
                        existingSubDict = jObj.ToObject<System.Collections.Generic.Dictionary<string, object>>();
                        layerDict[kvp.Key] = existingSubDict; // Replace JObject with Dictionary
                    }

                    if (existingSubDict != null)
                    {
                        // Ensure sub-dictionary has all required keys
                        foreach (var subKvp in defaultSubDict)
                        {
                            if (!existingSubDict.ContainsKey(subKvp.Key))
                            {
                                existingSubDict[subKvp.Key] = subKvp.Value;
                            }
                        }
                    }
                    else
                    {
                        // Replace invalid sub-dictionary
                        layerDict[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        private static void WriteJsonToFile<T>(string filePath, T data)
        {
            using (var writer = new StreamWriter(filePath))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = Formatting.Indented;
                var serializer = JsonSerializer.Create();
                serializer.Serialize(jsonWriter, data);
            }
        }

        private static T ReadJsonFromFile<T>(string filePath) where T : new()
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = JsonSerializer.Create();
                    return serializer.Deserialize<T>(jsonReader) ?? new T();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to read JSON from {filePath}: {ex.Message}");
                return new T();
            }
        }
    }
}
