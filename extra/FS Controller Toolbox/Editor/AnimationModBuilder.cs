using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UMod.ModTools.Export;
using UMod.BuildEngine;
using System.Text;
using System.Threading.Tasks;

namespace FlameStream
{
    [System.Serializable]
    public class ReferenceNode
    {
        public string name;
        public bool isExpanded;
        public List<ReferenceNode> children;

        public ReferenceNode(string name, List<ReferenceNode> children = null)
        {
            this.name = name;
            this.children = children ?? new List<ReferenceNode>();
            this.isExpanded = false;
        }
    }

    [System.Serializable]
    public class Config
    {
        public string uModAuthor = "FlameStream";
        public string uModExportPath = "E:/SteamLibrary/steamapps/common/Warudo/Warudo_Data/StreamingAssets/CharacterAnimations";
        public string uModGeneratedPath = "FS Controller Toolbox/uMod Assets";
        public string uModNamePrefix = "🔥🎮 ";
        public string uModTempSettingsPath = "FS Controller Toolbox/uMod Temp Settings";
        public string uModVersion = "1.0.0";
        public string animationClipsPath = "FS Controller Toolbox/Animation Clips";

        // Single root reference tree structure
        public ReferenceNode referenceTree;

        // Constructor to initialize with default tree
        public Config()
        {
            InitializeDefaultReferenceTree();
        }

        // Ensure tree is initialized when accessed
        public void EnsureTreeInitialized()
        {
            if (referenceTree == null)
            {
                InitializeDefaultReferenceTree();
            }
        }

        public void InitializeDefaultReferenceTree()
        {
            // Create single root node with default reference file
            referenceTree = new ReferenceNode("SWPro Base (Hold Controller).anim", new List<ReferenceNode>
            {
                new ReferenceNode("SWPro Btn A Hover.anim", new List<ReferenceNode>
                {
                    new ReferenceNode("SWPro Btn A Press.anim")
                }),
                new ReferenceNode("SWPro Btn B Hover.anim", new List<ReferenceNode>
                {
                    new ReferenceNode("SWPro Btn B Press.anim")
                }),
                new ReferenceNode("SWPro Btn X Hover.anim", new List<ReferenceNode>
                {
                    new ReferenceNode("SWPro Btn X Press.anim")
                }),
                new ReferenceNode("SWPro Btn Y Hover.anim", new List<ReferenceNode>
                {
                    new ReferenceNode("SWPro Btn Y Press.anim")
                }),
                new ReferenceNode("SWPro D Base.anim", new List<ReferenceNode>
                {
                    new ReferenceNode("SWPro D1 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D1 Press.anim") }),
                    new ReferenceNode("SWPro D2 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D2 Press.anim") }),
                    new ReferenceNode("SWPro D3 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D3 Press.anim") }),
                    new ReferenceNode("SWPro D4 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D4 Press.anim") }),
                    new ReferenceNode("SWPro D5 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D5 Press.anim") }),
                    new ReferenceNode("SWPro D6 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D6 Press.anim") }),
                    new ReferenceNode("SWPro D7 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D7 Press.anim") }),
                    new ReferenceNode("SWPro D8 Hover.anim", new List<ReferenceNode> { new ReferenceNode("SWPro D8 Press.anim") })
                })
            });
        }
    }

    public class AnimationModBuilder
    {
        private static Config _config;
        internal static Config Config
        {
            get
            {
                if (_config == null)
                {
                    _config = LoadConfig();
                }
                return _config;
            }
        }

        // Properties that access the config values
        private static string UModAuthor => Config.uModAuthor;
        private static string UModExportPath => Config.uModExportPath;
        private static string UModNamePrefix => Config.uModNamePrefix;
        private static string UModVersion => Config.uModVersion;
        private static string AnimationClipsPath => $"Assets/{Config.animationClipsPath}";
        private static string UModTempSettingsPath => $"Assets/{Config.uModTempSettingsPath}";
        private static string UModGeneratedPath => $"Assets/{Config.uModGeneratedPath}";
        private static string DefaultReferenceClip => Config.referenceTree?.name ?? "SWPro Base (Hold Controller).anim";

        // Constants for clip file names
        private const string ClipFileNameAnimation = "Animation.anim";
        private const string ClipFileNameReference = "Ref.anim";



        private const string ConfigKey = "FlameStream.FsToolbox";

        private static Config LoadConfig()
        {
            string json = EditorPrefs.GetString(ConfigKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<Config>(json);
                }
                catch
                {
                    // If JSON is corrupted, return default config
                }
            }
            return new Config();
        }

        private static void SaveConfig()
        {
            string json = JsonUtility.ToJson(_config, true);
            EditorPrefs.SetString(ConfigKey, json);
        }

        internal static void UpdateConfig(Config newConfig)
        {
            _config = newConfig;
            SaveConfig();
        }

        [MenuItem("FS Toolbox/Generate Mod Assets from Animation Clips", priority = 1)]
        public static async void GenerateUmodAssetsFromAnimationClips()
        {
            LogWindow.ShowWindow("Generate Mod Assets from Animation Clips");
            LogWindow.ClearLogs();

            // Give the window time to load and render properly
            await Task.Delay(150);

            await GenerateUModAssetsAsync();
        }

        [MenuItem("FS Toolbox/Build Warudo mods...", priority = 1)]
        public static void BuildWarudoMods()
        {
            // Get list of mods first
            if (!Directory.Exists(UModGeneratedPath))
            {
                EditorUtility.DisplayDialog("Error", $"Output root directory does not exist: [{UModGeneratedPath}]", "OK");
                return;
            }

            string[] directories = Directory.GetDirectories(UModGeneratedPath);
            List<string> modNames = directories.Select(dir => Path.GetFileName(dir)).ToList();

            if (modNames.Count == 0)
            {
                EditorUtility.DisplayDialog("No Mods Found", "No animation mod folders found to build.", "OK");
                return;
            }

            // Show selection window
            ModSelectionWindow.ShowWindow(modNames, directories);
        }



        [MenuItem("FS Toolbox/Configure...", priority = 1)]
        public static void ShowConfigWindow()
        {
            ConfigWindow.ShowWindow();
        }



        private static async Task GenerateUModAssetsAsync()
        {
            LogWindow.Log("Starting export clip processing...");

            LogWindow.Log($"Searching for animation clips in [{AnimationClipsPath}]...");
            string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationClipsPath });
            List<string> processedMods = new List<string>();

            LogWindow.Log($"Found {animGuids.Length} animation clips to process...");
            LogWindow.UpdateProgress(0f, "Processing animation clips...");

            for (int i = 0; i < animGuids.Length; i++)
            {
                string guid = animGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".anim"))
                    continue;

                string fileName = Path.GetFileName(assetPath);
                string animName = Path.GetFileNameWithoutExtension(fileName);
                string progressPrefix = $"[{i + 1}/{animGuids.Length}]";

                // SPECIAL: Handle base reference clip uniquely (it has no reference clip)
                bool isBaseRefClip = fileName == DefaultReferenceClip;
                if (isBaseRefClip)
                {
                    string refFolder = Path.Combine(UModGeneratedPath, animName);
                    Directory.CreateDirectory(refFolder);

                    string baseAnimTargetPath = Path.Combine(UModGeneratedPath, animName, ClipFileNameAnimation);
                    AssetDatabase.CopyAsset(assetPath, baseAnimTargetPath);

                    processedMods.Add(animName);
                    LogWindow.Log($"{progressPrefix} Processed base clip: [{animName}]");

                    // Update progress
                    float progress = (float)(i + 1) / animGuids.Length;
                    LogWindow.UpdateProgress(progress, $"Processing animation clips... ({i + 1}/{animGuids.Length})");

                    // Yield control to allow UI updates
                    await Task.Yield();
                    continue;
                }

                string targetFolder = Path.Combine(UModGeneratedPath, animName);
                Directory.CreateDirectory(targetFolder);

                string animTargetPath = Path.Combine(UModGeneratedPath, animName, ClipFileNameAnimation);
                AssetDatabase.CopyAsset(assetPath, animTargetPath);

                // Determine the correct reference clip for this animation
                string refClipName = GetReferenceClipName(animName);
                string refSourcePath = Path.Combine(AnimationClipsPath, refClipName);
                string refTargetPath = Path.Combine(UModGeneratedPath, animName, ClipFileNameReference);

                if (File.Exists(refSourcePath))
                {
                    AssetDatabase.CopyAsset(refSourcePath, refTargetPath);
                }
                else
                {
                    LogWindow.LogError($"Reference clip not found: [{refClipName}] for animation: [{animName}]");
                    return;
                }

                AnimationClip animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animTargetPath);
                AnimationClip refClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(refTargetPath);

                if (animClip != null && refClip != null)
                {
                    var settings = AnimationUtility.GetAnimationClipSettings(animClip);
                    settings.hasAdditiveReferencePose = true;
                    settings.additiveReferencePoseClip = refClip;
                    AnimationUtility.SetAnimationClipSettings(animClip, settings);

                    processedMods.Add(animName);
                    LogWindow.Log($"{progressPrefix} Processed [{animName}] with reference clip [{refClipName}]");

                    // Update progress
                    float progress = (float)(i + 1) / animGuids.Length;
                    LogWindow.UpdateProgress(progress, $"Processing animation clips... ({i + 1}/{animGuids.Length})");

                    // Yield control to allow UI updates
                    await Task.Yield();
                }
                else
                {
                    LogWindow.LogError($"Failed to load Animation or Ref clip for {animName}");
                    return;
                }
            }

            // Single refresh at the very end
            AssetDatabase.Refresh();
            LogWindow.HideProgress();
            LogWindow.Log($"Task completed. Processed {processedMods.Count} animation mods.");
        }

        private static Dictionary<string, string> BuildParentMapFromConfig()
        {
            var parentMap = new Dictionary<string, string>();
            Config.EnsureTreeInitialized();
            if (Config.referenceTree != null)
            {
                BuildParentMapRecursive(Config.referenceTree, null, parentMap);
            }
            return parentMap;
        }

        private static void BuildParentMapRecursive(ReferenceNode node, string parent, Dictionary<string, string> parentMap)
        {
            // Record parent relationship
            if (parent != null)
            {
                parentMap[node.name] = parent;
            }

            // Process children recursively
            if (node.children != null && node.children.Count > 0)
            {
                foreach (var child in node.children)
                {
                    BuildParentMapRecursive(child, node.name, parentMap);
                }
            }
        }

        private static string GetReferenceClipName(string animName)
        {
            var parentMap = BuildParentMapFromConfig();
            return parentMap.TryGetValue(animName + ".anim", out string parentClip) ? parentClip : DefaultReferenceClip;
        }

        public static string GetUModName(string name)
        {
            return UModNamePrefix + name;
        }

        public static async Task CreateAndBuildModsAsync(List<string> modNames)
        {
            if (modNames.Count == 0)
            {
                LogWindow.Log("No mods to build.");
                return;
            }

            try
            {
                for (int i = 0; i < modNames.Count; i++)
                {
                    string modName = modNames[i];
                    string progressPrefix = $"[{i + 1}/{modNames.Count}]";
                    LogWindow.Log($"{progressPrefix} Creating and building temporary mod: [{modName}]");

                    // Update progress
                    float progress = (float)(i + 1) / modNames.Count;
                    LogWindow.UpdateProgress(progress, $"Building mods... ({i + 1}/{modNames.Count})");

                    await BuildTemporaryModAsync(modName);

                    // Yield control to allow UI updates
                    await Task.Yield();
                }

                LogWindow.Log($"Completed building {modNames.Count} mods.");
            }
            catch (System.Exception ex)
            {
                LogWindow.LogError($"Error in CreateAndBuildMods: {ex.Message}");
            }
        }

        private static async Task BuildTemporaryModAsync(string modName)
        {
            string modAssetPath = Path.Combine(UModGeneratedPath, modName).Replace('\\', '/');
            string modDisplayName = GetUModName(modName);
            string tempSettingsPath = Path.Combine(UModTempSettingsPath, $"{modName}.asset");

            try
            {
                // Ensure the temp directory exists
                string tempDir = UModTempSettingsPath;
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                string tempYaml = CreateTemporaryModYaml(modName, modDisplayName, modAssetPath);
                File.WriteAllText(tempSettingsPath, tempYaml);
                LogWindow.Log($"Created temporary settings at [{tempSettingsPath}]");
                AssetDatabase.ImportAsset(tempSettingsPath);

                // Load the temporary settings
                ExportSettings tempSettings = AssetDatabase.LoadAssetAtPath<ExportSettings>(tempSettingsPath);
                if (tempSettings != null)
                {
                    LogWindow.Log($"Building mod [{modDisplayName}]");
                    ModToolsUtil.StartBuild(tempSettings);

                    // Use async delay instead of Thread.Sleep
                    await Task.Delay(500);
                }
                else
                {
                    LogWindow.LogError($"Failed to load temporary settings for [{modName}] at path [{tempSettingsPath}]");
                }
            }
            catch (System.Exception ex)
            {
                LogWindow.LogError($"Failed to build temporary mod [{modName}] at path [{tempSettingsPath}]: {ex.Message}");
            }
        }

        private static string CreateTemporaryModYaml(string modName, string modDisplayName, string modAssetPath)
        {
            return $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: -2028264498, guid: 4d9ac0cbb6dcbc8428255d2df10dafee, type: 3}}
  m_Name: {modName}
  m_EditorClassIdentifier:
  activeProfile: 0
  exportProfiles:
  - referencePaths: []
    modName: {modDisplayName}
    modAuthor: {UModAuthor}
    modVersion: {UModVersion}
    modDescription: Generated animation mod
    modAssetPath: {modAssetPath}
    modExportPath: {UModExportPath}
    modIcon: {{fileID: 0}}
  logLevel: 3
  optimizeMode: 0
  compressionLevel: 2
  clearConsoleOnBuild: 1
  showOutputDirectory: 1
  buildAndRunInvokeMode: 0
  commandLineExecutable:
  remoteServer: ";
        }
    }

    public class EditorGUILayoutConstants
    {
        public const int LAYOUT_STANDARD_SPACING = 12;
        public const int ELEMENT_STANDARD_SPACING = 4;

        public static RectOffset LayoutStandardSpacingRect => new RectOffset(
            LAYOUT_STANDARD_SPACING,
            LAYOUT_STANDARD_SPACING,
            LAYOUT_STANDARD_SPACING,
            LAYOUT_STANDARD_SPACING
        );

        private static GUIStyle layoutStandardStyle;
        public static GUIStyle LayoutStandardStyle
        {
            get
            {
                if (layoutStandardStyle == null)
                {
                    layoutStandardStyle = new GUIStyle();
                    layoutStandardStyle.padding = LayoutStandardSpacingRect;
                }
                return layoutStandardStyle;
            }
        }
    }

    public class ModSelectionWindow : EditorWindow
    {
        private List<ModItem> modItems = new List<ModItem>();
        private Vector2 scrollPosition;

        [System.Serializable]
        public class ModItem
        {
            public string name;
            public bool selected;
            public string path;

            public ModItem(string name, string path)
            {
                this.name = name;
                this.path = path;
                this.selected = true;
            }
        }

        public static void ShowWindow(List<string> modNames, string[] modPaths)
        {
            ModSelectionWindow window = GetWindow<ModSelectionWindow>(true, "Select Mods to Build");
            window.minSize = new Vector2(400, 300);
            // window.maxSize = new Vector2(600, 500);

            window.modItems.Clear();
            for (int i = 0; i < modNames.Count; i++)
            {
                window.modItems.Add(new ModItem(modNames[i], modPaths[i]));
            }

            window.Show();
        }

        private void SelectAllItems()
        {
            foreach (var item in modItems)
                item.selected = true;
        }

        private void SelectNoneItems()
        {
            foreach (var item in modItems)
                item.selected = false;
        }

        private void OnGUI()
        {
            // -- HEADER --
            EditorGUILayout.BeginVertical(EditorGUILayoutConstants.LayoutStandardStyle);
            {
                // Heading Text
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{modItems.Count} mod assets found", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                // Select All/None buttons
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select All", GUILayout.Width(100), GUILayout.Height(24))) { SelectAllItems();}

                    GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                    if (GUILayout.Button("Select None", GUILayout.Width(100), GUILayout.Height(24))) { SelectNoneItems(); }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            // -- BODY --

            // Scrollable list of mods with dark background
            GUIStyle darkBoxStyle = new GUIStyle(GUI.skin.textArea);
            darkBoxStyle.padding = new RectOffset(8, 1, 1, 1);
            EditorGUILayout.BeginVertical(darkBoxStyle, GUILayout.ExpandHeight(true));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
            foreach (var item in modItems)
            {
                EditorGUILayout.BeginHorizontal();
                item.selected = EditorGUILayout.Toggle(item.selected, GUILayout.Width(20));
                EditorGUILayout.LabelField(AnimationModBuilder.GetUModName(item.name));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // -- FOOTER --
            EditorGUILayout.BeginHorizontal(EditorGUILayoutConstants.LayoutStandardStyle);
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(24))) { Close(); }

                GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                var selectedItems = modItems.Where(item => item.selected).ToList();
                string buildButtonText = selectedItems.Count > 0 ? $"Build ({selectedItems.Count})" : "Build (0)";
                EditorGUI.BeginDisabledGroup(selectedItems.Count == 0);
                {
                    if (GUILayout.Button(buildButtonText, GUILayout.Width(100), GUILayout.Height(24))) { StartBuildProcess(selectedItems); Close(); }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private async void StartBuildProcess(List<ModItem> selectedItems)
        {
            List<string> selectedModNames = selectedItems.Select(item => item.name).ToList();

            LogWindow.ShowWindow("Build Selected UMotion UMod mods");
            LogWindow.ClearLogs();
            await Task.Delay(150);

            LogWindow.Log($"Building {selectedModNames.Count} selected mods...");
            LogWindow.UpdateProgress(0f, "Building selected mods...");
            await AnimationModBuilder.CreateAndBuildModsAsync(selectedModNames);
            LogWindow.HideProgress();
            LogWindow.Log("Build task completed.");
        }
    }

    public class LogWindow : EditorWindow
    {
        private static LogWindow window;
        private static StringBuilder logBuilder = new StringBuilder();
        private Vector2 scrollPosition;
        private string windowTitle = "UMotion Task Log";
        private static float progressValue = 0f;
        private static string progressText = "";
        private static bool showProgress = false;

        public static void ShowWindow(string taskName)
        {
            window = GetWindow<LogWindow>(true, taskName);
            window.windowTitle = taskName;
            window.minSize = new Vector2(400, 300);
            window.Show();
            window.Focus();
        }

        public static void ClearLogs()
        {
            logBuilder.Clear();
            progressValue = 0f;
            progressText = "Initializing...";
            showProgress = true;
            if (window != null)
                window.Repaint();
        }

        public static void UpdateProgress(float progress, string text)
        {
            progressValue = progress;
            progressText = text;
            showProgress = true;
            if (window != null)
                window.Repaint();
        }

        public static void HideProgress()
        {
            showProgress = false;
            if (window != null)
                window.Repaint();
        }

        public static void Log(string message)
        {
            logBuilder.AppendLine($"[{System.DateTime.Now:HH:mm:ss}] {message}");
            if (window != null)
            {
                window.ScrollToBottom();
                window.Repaint();
                // Force immediate UI update
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        public static void LogError(string message)
        {
            logBuilder.AppendLine($"[{System.DateTime.Now:HH:mm:ss}] ERROR: {message}");

            // Also log in Unity console for stack traces
            Debug.LogError(message);

            if (window != null)
            {
                window.ScrollToBottom();
                window.Repaint();
                // Force immediate UI update
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private void ScrollToBottom()
        {
            scrollPosition.y = float.MaxValue;
        }

        private void OnGUI()
        {
            // -- PROGRESS SECTION --
            if (showProgress)
            {
                EditorGUILayout.BeginVertical(EditorGUILayoutConstants.LayoutStandardStyle);
                {
                    if (!string.IsNullOrEmpty(progressText))
                    {
                        EditorGUILayout.LabelField(progressText, EditorStyles.label);
                    }

                    GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                    Rect progressRect = EditorGUILayout.GetControlRect(false, 18);
                    EditorGUI.ProgressBar(progressRect, progressValue, $"{(progressValue * 100):F1}%");
                }
                EditorGUILayout.EndVertical();
            }

            // -- LOG CONTENT --
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(logBuilder.ToString(), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            // -- FOOTER --
            EditorGUILayout.BeginHorizontal(EditorGUILayoutConstants.LayoutStandardStyle);
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Close", GUILayout.Width(80), GUILayout.Height(24))) { Close(); }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnDestroy()
        {
            window = null;
        }
    }

    public class ConfigWindow : EditorWindow
    {
        private Config tempConfig;
        private Vector2 scrollPosition;
        private Vector2 treeScrollPosition; // Add separate scroll for tree view
        private bool showReferenceTreeHelp = false; // Track help visibility

        private const int FOLDOUT_WIDTH = 12;
        private const int INDENT_PER_LEVEL = 16;
        private const int BUTTON_WIDTH = 22;

        public static void ShowWindow()
        {
            ConfigWindow window = GetWindow<ConfigWindow>(true, "FS Toolbox Configuration");
            window.minSize = new Vector2(550, 410);

            // Ensure config tree is initialized before copying
            AnimationModBuilder.Config.EnsureTreeInitialized();

            // Create a copy of the current config for editing
            window.tempConfig = JsonUtility.FromJson<Config>(
                JsonUtility.ToJson(AnimationModBuilder.Config)
            );

            // Ensure the copied config also has initialized tree
            if (window.tempConfig.referenceTree == null)
            {
                window.tempConfig.InitializeDefaultReferenceTree();
            }

            window.Show();
        }

        private void DrawProjectFolderField(string label, ref string path, string panelTitle)
        {
            EditorGUILayout.BeginHorizontal();
            {
                path = EditorGUILayout.TextField(label, path);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string fullPath = string.IsNullOrEmpty(path) ? Application.dataPath : Path.Combine(Application.dataPath, path);
                    string selectedPath = EditorUtility.OpenFolderPanel(panelTitle, fullPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // Convert to relative path without Assets/ prefix if it's within the project
                        if (selectedPath.StartsWith(Application.dataPath))
                        {
                            path = selectedPath.Substring(Application.dataPath.Length + 1); // Remove dataPath + "/"
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Path", "Please select a folder within the Assets directory.", "OK");
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReferenceTreeNode(ReferenceNode node, int indentLevel)
        {
            // For root node, just call the deletion-aware method but ignore the return
            DrawReferenceTreeNodeWithDeletion(node, indentLevel);
        }

        private void DrawReferenceTreeChildren(List<ReferenceNode> children, int indentLevel)
        {
            if (children == null) return;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];

                if (DrawReferenceTreeNodeWithDeletion(child, indentLevel))
                {
                    // Child was marked for deletion
                    children.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }
        }

        private bool DrawReferenceTreeNodeWithDeletion(ReferenceNode node, int indentLevel)
        {
            if (node == null) return false;

            bool hasChildren = node.children != null && node.children.Count > 0;
            bool shouldDelete = false;

            EditorGUILayout.BeginHorizontal();
            {
                // Base indentation for hierarchy levels
                if (indentLevel > 0)
                {
                    GUILayout.Space(indentLevel * INDENT_PER_LEVEL);
                }

                // Combined foldout and text field in same horizontal space
                if (hasChildren)
                {
                    // Render foldout icon
                    Rect lineRect = EditorGUILayout.GetControlRect();
                    Rect foldoutRect = new Rect(lineRect.x, lineRect.y, FOLDOUT_WIDTH + EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING, lineRect.height);
                    Rect textRect = new Rect(lineRect.x + FOLDOUT_WIDTH + EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING, lineRect.y, lineRect.width - FOLDOUT_WIDTH - EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING, lineRect.height);

                    bool wasExpanded = node.isExpanded;
                    node.isExpanded = EditorGUI.Foldout(foldoutRect, node.isExpanded, "");
                    node.name = EditorGUI.TextField(textRect, node.name);

                    // If expansion state changed, ensure proper display
                    if (wasExpanded != node.isExpanded)
                    {
                        GUI.changed = true;
                    }
                }
                else
                {
                    // For leaf nodes, reserve same space as foldout for alignment
                    GUILayout.Space(FOLDOUT_WIDTH + EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);
                    // Text field for leaf nodes
                    node.name = EditorGUILayout.TextField(node.name, GUILayout.MinWidth(100));
                }


                // Browse button for selecting animation file
                if (GUILayout.Button("Browse", GUILayout.Width(60), GUILayout.Height(16)))
                {
                    string fullPath = string.IsNullOrEmpty(tempConfig.animationClipsPath) ? Application.dataPath : Path.Combine(Application.dataPath, tempConfig.animationClipsPath);
                    string selectedPath = EditorUtility.OpenFilePanel("Select Animation File", fullPath, "anim");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // Convert to just the filename if it's within the clips folder
                        string clipsFolderPath = Path.Combine(Application.dataPath, tempConfig.animationClipsPath).Replace('\\', '/');
                        if (selectedPath.StartsWith(clipsFolderPath))
                        {
                            node.name = Path.GetFileName(selectedPath);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid File", $"Please select a file within the Animation Clip Location ({clipsFolderPath}).", "OK");
                        }
                    }
                }

                if (GUILayout.Button("+", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(16)))
                {
                    if (node.children == null) node.children = new List<ReferenceNode>();
                    node.children.Add(new ReferenceNode(""));
                    node.isExpanded = true;
                }

                // Only allow deletion of non-root nodes
                if (indentLevel > 0)
                {
                    if (GUILayout.Button("-", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(16)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Node", $"Delete '{node.name}' and all its children?", "Delete", "Cancel"))
                        {
                            shouldDelete = true;
                        }
                    }
                }
                else
                {
                    // For root node, add spacing equivalent to delete button for alignment
                    GUILayout.Space(BUTTON_WIDTH + 3);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Draw children if expanded (recursive call with increased indent level)
            if (hasChildren && node.isExpanded)
            {
                DrawReferenceTreeChildren(node.children, indentLevel + 1);
            }

            return shouldDelete;
        }        private void OnGUI()
        {
            // Store original label width and set wider label width for better text display
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 200f;

            // -- CONTENT --
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(EditorGUILayoutConstants.LayoutStandardStyle, GUILayout.ExpandHeight(true));
            {
                // Create section style with dark background
                GUIStyle sectionStyle = new GUIStyle();
                sectionStyle.padding = EditorGUILayoutConstants.LayoutStandardSpacingRect;
                Texture2D backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0.22f, 0.22f, 0.22f, 1f));
                backgroundTexture.Apply();
                sectionStyle.normal.background = backgroundTexture;

                // -- Project Settings --
                EditorGUILayout.BeginVertical(sectionStyle);
                {
                    EditorGUILayout.LabelField("Animation Clip Handler Settings", EditorStyles.boldLabel);
                    GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                    DrawProjectFolderField("Animation Clip Location", ref tempConfig.animationClipsPath, "Select Animation Clip Folder");
                    DrawProjectFolderField("Generated Mod Assets Location", ref tempConfig.uModGeneratedPath, "Select Generated Mod Assets Location");
                    DrawProjectFolderField("Temp Settings Folder", ref tempConfig.uModTempSettingsPath, "Select UMod Temp Settings Location");
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                // -- Mod Settings --
                EditorGUILayout.BeginVertical(sectionStyle);
                {
                    EditorGUILayout.LabelField("Mod Settings", EditorStyles.boldLabel);
                    GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                    tempConfig.uModAuthor = EditorGUILayout.TextField("Author", tempConfig.uModAuthor);
                    tempConfig.uModVersion = EditorGUILayout.TextField("Version", tempConfig.uModVersion);
                    tempConfig.uModNamePrefix = EditorGUILayout.TextField("Name Prefix", tempConfig.uModNamePrefix);

                    EditorGUILayout.BeginHorizontal();
                    {
                        tempConfig.uModExportPath = EditorGUILayout.TextField("Export Path", tempConfig.uModExportPath);
                        if (GUILayout.Button("Browse", GUILayout.Width(60)))
                        {
                            string path = EditorUtility.OpenFolderPanel("Select UMod Export Path", tempConfig.uModExportPath, "");
                            if (!string.IsNullOrEmpty(path))
                            {
                                tempConfig.uModExportPath = path;
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                // -- Reference Tree Section --
                EditorGUILayout.BeginVertical(sectionStyle);
                {
                    // Title with help icon
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Animation Reference Tree", EditorStyles.boldLabel);

                        if (GUILayout.Button("?", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(16)))
                        {
                            showReferenceTreeHelp = !showReferenceTreeHelp;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                    // Help text section (toggleable)
                    if (showReferenceTreeHelp)
                    {
                        GUIStyle helpStyle = new GUIStyle(EditorStyles.helpBox);
                        helpStyle.padding = EditorGUILayoutConstants.LayoutStandardSpacingRect;
                        EditorGUILayout.BeginVertical(helpStyle);
                        {
                            EditorGUILayout.LabelField("How Animation Reference Tree Works:", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField("• Root Node: Required. The default reference clip (no additive blending applied)", EditorStyles.wordWrappedLabel);
                            EditorGUILayout.LabelField("• Child Nodes: Use their parent clip as additive reference for blending", EditorStyles.wordWrappedLabel);
                            EditorGUILayout.LabelField("• Undefined Animations: Will automatically use the root node as reference", EditorStyles.wordWrappedLabel);
                        }
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);
                    }

                    // Tree view area with flexible height that grows to fill remaining space
                    GUIStyle treeAreaStyle = new GUIStyle(GUI.skin.box);
                    treeAreaStyle.padding = EditorGUILayoutConstants.LayoutStandardSpacingRect;
                    treeScrollPosition = EditorGUILayout.BeginScrollView(treeScrollPosition);
                    EditorGUILayout.BeginVertical(treeAreaStyle, GUILayout.ExpandHeight(true));
                    {
                        if (tempConfig.referenceTree != null)
                        {
                            DrawReferenceTreeNode(tempConfig.referenceTree, 0);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("No reference tree configured.", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();

            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            // -- FOOTER --
            EditorGUILayout.BeginHorizontal(EditorGUILayoutConstants.LayoutStandardStyle);
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120), GUILayout.Height(24)))
                {
                    tempConfig = new Config();
                }

                GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(24)))
                {
                    Close();
                }

                GUILayout.Space(EditorGUILayoutConstants.ELEMENT_STANDARD_SPACING);

                if (GUILayout.Button("Save", GUILayout.Width(80), GUILayout.Height(24)))
                {
                    AnimationModBuilder.UpdateConfig(tempConfig);
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Restore original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }
    }
}
