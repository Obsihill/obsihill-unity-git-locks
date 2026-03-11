// <copyright file="GitLocksDisplay.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
public class GitLocksDisplay : EditorWindow
{
    private static Texture greenLockIcon;
    private static Texture orangeLockIcon;
    private static Texture redLockIcon;
    private static Texture mixedLockIcon;

    private static List<GitLocksObject> selectedLocks;

    private Label refreshTimeLabel;
    private ScrollView myLocksScrollView;
    private ScrollView otherLocksScrollView;
    private VisualElement disabledContainer;
    private VisualElement setupContainer;
    private VisualElement mainToolContainer;

    // Show git history
    private static readonly int showHistoryMaxNumOfFilesBeforeWarning = 5;

    static GitLocksDisplay()
    {
        // Add our own GUI to the project and hierarchy windows
        EditorApplication.projectWindowItemOnGUI += DrawProjectLocks;
        EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyLocks;
    }

    [MenuItem("Window/Git Locks")]
    public static void ShowWindow()
    {
        // Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(GitLocksDisplay), false, "Git Locks");
    }

    public static void RepaintAll()
    {
        EditorApplication.RepaintHierarchyWindow();
        EditorApplication.RepaintProjectWindow();
        if (EditorWindow.HasOpenInstances<GitLocksDisplay>())
        {
            GitLocksDisplay locksWindow = GetWindow<GitLocksDisplay>("Git Locks", false);
            locksWindow.RefreshUI();
            locksWindow.Repaint();
        }
    }

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

    public static Texture GetIconForLockedObject(GitLocksObject lo)
    {
        if (lo == null)
        {
            return null;
        }

        bool isLockConflictingWithUncommitedFile = GitLocks.IsLockedObjectConflictingWithUncommitedFile(lo);

        if (lo.IsMine())
        {
            return GetGreenLockIcon();
        }
        else if (isLockConflictingWithUncommitedFile)
        {
            return GetRedLockIcon();
        }
        else
        {
            return GetOrangeLockIcon();
        }
    }

    public static Texture GetMixedLockIconForFolder()
    {
        return GetMixedLockIcon();
    }

    public static void DisplayLockIcon(string path, Rect selectionRect, float offset, bool small = false)
    {
        var frame = new Rect(selectionRect);

        // Handle files
        GitLocksObject lo = GitLocks.GetObjectInLockedCache(path);
        if (lo != null)
        {
            frame.x += offset + (small ? 3f : 0f);
            frame.width = small ? 12f : 18f;

            Texture lockTexture = GetIconForLockedObject(lo);
            string tooltip;

            // Fill tooltip
            if (lo.IsMine())
            {
                tooltip = "Locked by me";
            }
            else if (GitLocks.IsLockedObjectConflictingWithUncommitedFile(lo))
            {
                tooltip = "Conflicting with lock by " + lo.Owner.Name;
            }
            else
            {
                tooltip = "Locked by " + lo.Owner.Name;
            }

            if (GUI.Button(frame, new GUIContent(lockTexture, tooltip), GUI.skin.label))
            {
                if (lo.IsMine())
                {
                    if (!EditorUtility.DisplayDialog("Asset locked by you", "You have locked this asset, you're safe working on it.", "OK", "Unlock"))
                    {
                        GitLocks.UnlockFile(lo.Path);
                        GitLocks.RefreshLocks();
                    }
                }
                else if (GitLocks.IsLockedObjectConflictingWithUncommitedFile(lo))
                {
                    EditorUtility.DisplayDialog("Asset locked by someone else and conflicting", "User " + lo.Owner.Name + " has locked this asset (" + lo.GetLockDateTimeString() + ") and you have uncommited modifications: you should probably discard them as you won't be able to push them.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Asset locked by someone else", "User " + lo.Owner.Name + " has locked this asset (" + lo.GetLockDateTimeString() + "), you cannot work on it.", "OK");
                }
            }
        }

        // Handle folders
        if (Directory.Exists(path) && GitLocks.LockedObjectsCache != null)
        {
            bool containsOneOfMyLocks = false;
            bool containsOneOfOtherLocks = false;
            bool containsOneConflictingLock = false;
            foreach (GitLocksObject dlo in GitLocks.LockedObjectsCache)
            {
                string folderPath = path + "/";
                if (dlo.Path.Contains(folderPath))
                {
                    if (dlo.IsMine())
                    {
                        containsOneOfMyLocks = true;
                    }
                    else if (GitLocks.IsLockedObjectConflictingWithUncommitedFile(dlo))
                    {
                        containsOneConflictingLock = true;
                        containsOneOfOtherLocks = true;
                    }
                    else
                    {
                        containsOneOfOtherLocks = true;
                    }
                    if (containsOneOfMyLocks && containsOneOfOtherLocks)
                    {
                        break;
                    }
                }
            }

            if (containsOneOfMyLocks || containsOneOfOtherLocks)
            {
                frame.x += offset + 15;
                frame.width = 15f;
                string tooltip;
                Texture lockTexture;

                if (containsOneOfMyLocks && containsOneOfOtherLocks)
                {
                    lockTexture = GetMixedLockIcon();
                    tooltip = "Folder contains files locked by me and others";
                }
                else if (containsOneOfMyLocks)
                {
                    lockTexture = GetGreenLockIcon();
                    tooltip = "Folder contains files locked by me";
                }
                else if (containsOneConflictingLock)
                {
                    lockTexture = GetRedLockIcon();
                    tooltip = "Folder contains conflicting files";
                }
                else
                {
                    lockTexture = GetOrangeLockIcon();
                    tooltip = "Folder contains files locked by others";
                }

                GUI.Button(frame, new GUIContent(lockTexture, tooltip), GUI.skin.label);
            }
        }
    }

    // -----------------------
    // Project window features
    // -----------------------
    [MenuItem("Assets/Git LFS Lock %#l", false, 1100)]
    private static void ItemMenuLock()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.DeepAssets);
        List<string> paths = new ();
        foreach (UnityEngine.Object o in selected)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                continue; // Folders are not lockable, skip this asset
            }

            paths.Add(path);
        }

        GitLocks.LockFiles(paths);
        GitLocks.RefreshLocks();
    }

    [MenuItem("Assets/Git LFS Lock %#l", true)]
    private static bool ValidateItemMenuLock()
    {
        if (!GitLocks.IsEnabled())
        {
            return false; // Early return if the whole tool is disabled
        }

        // Don't allow if the locked cache hasn't been built
        if (GitLocks.LastRefresh <= DateTime.MinValue)
        {
            return false;
        }

        UnityEngine.Object[] selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
        foreach (UnityEngine.Object o in selected)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (Directory.Exists(path))
            {
                foreach (GitLocksObject lo in GitLocks.LockedObjectsCache)
                {
                    string folderPath = path + "/";
                    if (lo.Path.Contains(folderPath))
                    {
                        return false;
                    }
                }
            }
            else if (!GitLocks.IsObjectAvailableToLock(path))
            {
                return false;
            }
        }

        return true;
    }

    [MenuItem("Assets/Git LFS Unlock %#u", false, 1101)]
    private static void ItemMenuUnlock()
    {
        List<string> paths = new List<string>();
        UnityEngine.Object[] selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
        foreach (UnityEngine.Object o in selected)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (Directory.Exists(path))
            {
                foreach (GitLocksObject lo in GitLocks.LockedObjectsCache)
                {
                    string folderPath = path + "/";
                    if (lo.Path.Contains(folderPath))
                    {
                        if (GitLocks.IsObjectAvailableToUnlock(lo))
                        {
                            paths.Add(lo.Path);
                        }
                    }
                }
            }
            else if (GitLocks.IsObjectAvailableToUnlock(path))
            {
                paths.Add(path);
            }
        }

        GitLocks.UnlockFiles(paths);
        GitLocks.RefreshLocks();
    }

    [MenuItem("Assets/Git LFS Unlock %#u", true)]
    private static bool ValidateItemMenuUnlock()
    {
        if (!GitLocks.IsEnabled())
        {
            return false; // Early return if the whole tool is disabled
        }

        // Don't allow if the locked cache hasn't been built
        if (GitLocks.LastRefresh <= DateTime.MinValue)
        {
            return false;
        }

        UnityEngine.Object[] selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
        bool foundObjectToUnlock = false;
        foreach (UnityEngine.Object o in selected)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (Directory.Exists(path))
            {
                foreach (GitLocksObject lo in GitLocks.LockedObjectsCache)
                {
                    string folderPath = path + "/";
                    if (lo.Path.Contains(folderPath))
                    {
                        if (lo.IsMine())
                        {
                            foundObjectToUnlock = true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            else if (GitLocks.IsObjectAvailableToUnlock(path))
            {
                foundObjectToUnlock = true;
            }
        }

        return foundObjectToUnlock;
    }

    // -------------------------
    // Hierarchy window features
    // -------------------------
    [MenuItem("GameObject/Git LFS Lock", false, 40)]
    private static void ItemMenuLockHierarchy()
    {
        List<string> paths = SelectionToPaths();

        GitLocks.LockFiles(paths);

        // Clear the selection to make sure it's called only once
        Selection.objects = null;

        GitLocks.RefreshLocks();
    }

    [MenuItem("GameObject/Git LFS Lock", true)]
    private static bool ValidateItemMenuLockHierarchy()
    {
        if (!GitLocks.IsEnabled())
        {
            return false; // Early return if the whole tool is disabled
        }

        // Don't allow if the locked cache hasn't been built
        if (GitLocks.LastRefresh <= DateTime.MinValue)
        {
            return false;
        }

        List<string> paths = SelectionToPaths();

        if (paths.Count == 0)
        {
            return false;
        }

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!GitLocks.IsObjectAvailableToLock(path))
            {
                return false;
            }
        }

        return true;
    }

    [MenuItem("GameObject/Git LFS Unlock", false, 41)]
    private static void ItemMenuUnlockHierarchy()
    {
        List<string> paths = SelectionToPaths();

        GitLocks.UnlockFiles(paths);

        // Clear the selection to make sure it's called only once
        Selection.objects = null;

        GitLocks.RefreshLocks();
    }

    [MenuItem("GameObject/Git LFS Unlock", true)]
    private static bool ValidateItemMenuUnlockHierarchy()
    {
        if (!GitLocks.IsEnabled())
        {
            return false; // Early return if the whole tool is disabled
        }

        // Don't allow if the locked cache hasn't been built
        if (GitLocks.LastRefresh <= DateTime.MinValue)
        {
            return false;
        }

        List<string> paths = SelectionToPaths();

        if (paths.Count == 0)
        {
            return false;
        }

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!GitLocks.IsObjectAvailableToUnlock(path))
            {
                return false;
            }
        }

        return true;
    }

    // -------------------------------------------
    // Draw icons in hierarchy and project windows
    // -------------------------------------------
    private static void DrawProjectLocks(string guid, Rect selectionRect)
    {
        if (!GitLocks.IsEnabled())
        {
            return; // Early return if the whole tool is disabled
        }

        GitLocks.CheckLocksRefresh();

        string path = AssetDatabase.GUIDToAssetPath(guid);

        DisplayLockIcon(path, selectionRect, -12f);
    }

    private static void DrawHierarchyLocks(int instanceID, Rect selectionRect)
    {
        if (!GitLocks.IsEnabled())
        {
            return; // Early return if the whole tool is disabled
        }

        GitLocks.CheckLocksRefresh();

        string path = string.Empty;
        bool small = false;

        // Handle scenes
        path = GitLocks.GetSceneFromInstanceID(instanceID).path;

        // Handle prefabs
        string tmpPath = GitLocks.GetAssetPathFromPrefabGameObject(instanceID);
        if (tmpPath != string.Empty)
        {
            path = tmpPath;
            small = !GitLocks.IsObjectPrefabRoot(instanceID);
        }

        // Display
        if (path != string.Empty)
        {
            DisplayLockIcon(path, selectionRect, -30f, small);
        }
    }

    // ------------
    // File history
    // ------------
    [MenuItem("Assets/Show Git History", false, 1101)]
    private static void ItemMenuGitHistory()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);

        // Display a warning if you're about to open many CLIs or browser tabs to prevent slowing down your computer if you misclick
        if (selected.Length <= showHistoryMaxNumOfFilesBeforeWarning || EditorUtility.DisplayDialog("Are you sure?", "More than " + showHistoryMaxNumOfFilesBeforeWarning + " files have been selected, are you sure you want to open the history for all of them?", "Yes", "Cancel"))
        {
            foreach (UnityEngine.Object o in selected)
            {
                string path = AssetDatabase.GetAssetPath(o);
                if (Directory.Exists(path))
                {
                    continue; // Folders are not lockable, skip this asset
                }

                if (EditorPrefs.GetBool("gitLocksShowHistoryInBrowser", false))
                {
                    string url = EditorPrefs.GetString("gitLocksShowHistoryInBrowserUrl");
                    if (url != string.Empty && url.Contains("$branch") && url.Contains("$assetPath"))
                    {
                        url = url.Replace("$branch", GitLocks.GetCurrentBranch());
                        url = url.Replace("$assetPath", path);
                        UnityEngine.Application.OpenURL(url);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("URL was not formatted correctly to show the file's history in your browser: it must be formatted like https://github.com/MyUserName/MyRepo/blob/$branch/$assetPath");
                    }
                }
                else
                {
                    GitLocks.ExecuteProcessTerminal("git", "log \"" + path + "\"", true);
                }
            }
        }
    }

    [MenuItem("Assets/Show Git History", true)]
    private static bool ValidateItemMenuGitHistory()
    {
        UnityEngine.Object[] selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
        foreach (UnityEngine.Object o in selected)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (Directory.Exists(path))
            {
                return false;
            }
        }

        return true;
    }

    // -----------
    // Toolbox
    // -----------
    private static List<string> SelectionToPaths()
    {
        List<string> paths = new List<string>();

        Dictionary<int, Scene> _loadedScenesByHash = new Dictionary<int, Scene>();
        int countLoaded = SceneManager.sceneCount;
        for (int i = 0; i < countLoaded; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            _loadedScenesByHash[loadedScene.GetHashCode()] = loadedScene;
        }

        foreach (int instanceID in Selection.entityIds)
        {
            UnityEngine.Object obj = EditorUtility.EntityIdToObject(instanceID);
            if (obj != null)
            {
                string path = GitLocks.GetAssetPathFromPrefabGameObject(instanceID);
                paths.Add(path);
            }
            else if (_loadedScenesByHash.TryGetValue(instanceID, out Scene scene))
            {
                paths.Add(scene.path);
            }
        }

        return paths;
    }

    private class LockTreeNode
    {
        public string Name;
        public GitLocksObject LockItem;
        public Dictionary<string, LockTreeNode> Children = new Dictionary<string, LockTreeNode>();
    }

    private void BuildLockTree(ScrollView scrollView, List<GitLocksObject> locks, bool isMine)
    {
        scrollView.Clear();
        if (locks == null || locks.Count == 0) return;

        LockTreeNode rootNode = new LockTreeNode { Name = "Root" };
        foreach (var lo in locks)
        {
            string cleanPath = lo.Path.Replace("\\", "/");
            string[] parts = cleanPath.Split('/');
            LockTreeNode current = rootNode;
            
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!current.Children.ContainsKey(parts[i]))
                    current.Children[parts[i]] = new LockTreeNode { Name = parts[i] };
                current = current.Children[parts[i]];
            }
            current.Children[parts[parts.Length - 1]] = new LockTreeNode { Name = parts[parts.Length - 1], LockItem = lo };
        }

        foreach (var kvp in rootNode.Children)
        {
            BuildTreeNodeUI(scrollView, kvp.Value, isMine);
        }
    }

    private void BuildTreeNodeUI(VisualElement parent, LockTreeNode node, bool isMine)
    {
        if (node.LockItem != null)
        {
            parent.Add(MakeLockItemElement(node.LockItem, isMine));
        }
        else
        {
            var foldout = new Foldout();
            foldout.text = node.Name;
            foldout.value = true;

            var toggle = foldout.Q<Toggle>();
            if (toggle != null) toggle.style.unityFontStyleAndWeight = FontStyle.Bold;

            foreach (var childNode in node.Children.Values)
            {
                BuildTreeNodeUI(foldout, childNode, isMine);
            }
            parent.Add(foldout);
        }
    }

    private VisualElement MakeLockItemElement(GitLocksObject lo, bool isMine)
    {
        var container = new VisualElement();
        container.AddToClassList("lock-item");
        container.style.height = 25;

        var toggle = new Toggle();
        toggle.name = "toggle";
        toggle.AddToClassList("lock-item-toggle");
        if (isMine)
        {
            if (selectedLocks == null) selectedLocks = new List<GitLocksObject>();
            toggle.SetValueWithoutNotify(selectedLocks.Contains(lo));
            toggle.RegisterValueChangedCallback(evt => {
                if (evt.newValue && !selectedLocks.Contains(lo)) selectedLocks.Add(lo);
                else if (!evt.newValue && selectedLocks.Contains(lo)) selectedLocks.Remove(lo);
            });
        }
        else
        {
            toggle.style.display = DisplayStyle.None;
        }
        container.Add(toggle);

        var icon = new VisualElement();
        icon.AddToClassList("lock-item-icon");
        icon.style.backgroundImage = (Texture2D)GetIconForLockedObject(lo);
        container.Add(icon);

        var objField = new UnityEditor.UIElements.ObjectField();
        objField.objectType = typeof(UnityEngine.Object);
        objField.SetEnabled(false);
        objField.AddToClassList("lock-item-object");
        objField.value = lo.GetObjectReference();
        container.Add(objField);

        var pathLabel = new Label();
        pathLabel.AddToClassList("lock-item-path");
        pathLabel.text = lo.Path;
        pathLabel.tooltip = lo.Path;
        container.Add(pathLabel);

        var ownerLabel = new Label();
        ownerLabel.AddToClassList("lock-item-owner");
        ownerLabel.text = lo.Owner.Name;
        ownerLabel.tooltip = lo.GetLockDateTimeString();
        container.Add(ownerLabel);

        var unlockBtn = new Button();
        unlockBtn.text = "Unlock";
        unlockBtn.AddToClassList("lock-item-btn");
        unlockBtn.style.display = isMine ? DisplayStyle.Flex : DisplayStyle.None;
        unlockBtn.clicked += () => {
            GitLocks.UnlockFile(lo.Path);
            GitLocks.RefreshLocks();
        };
        container.Add(unlockBtn);

        var forceUnlockBtn = new Button();
        forceUnlockBtn.text = "Force unlock";
        forceUnlockBtn.AddToClassList("lock-item-force-btn");
        forceUnlockBtn.style.display = EditorPrefs.GetBool("gitLocksShowForceButtons") ? DisplayStyle.Flex : DisplayStyle.None;
        forceUnlockBtn.clicked += () => {
            if (EditorUtility.DisplayDialog("Force unlock ?", "Are you sure you want to force the unlock ? It may mess with a teammate's work !", "Yes, I know the risks", "Cancel, I'm not sure")) {
                GitLocks.UnlockFile(lo.Path, true);
                GitLocks.RefreshLocks();
            }
        };
        container.Add(forceUnlockBtn);

        return container;
    }

    private static Texture LoadIcon(string filename)
    {
        Texture tex = (Texture)AssetDatabase.LoadAssetAtPath("Packages/com.tomduchene.unity-git-locks/Editor/Textures/" + filename, typeof(Texture));
        if (tex == null)
            tex = (Texture)AssetDatabase.LoadAssetAtPath("Assets/unity-git-locks-main/Editor/Textures/" + filename, typeof(Texture));
        return tex;
    }

    private static Texture GetGreenLockIcon(bool forceReload = false)
    {
        if (greenLockIcon == null || forceReload)
        {
            bool cb = EditorPrefs.HasKey("gitLocksColorblindMode") && EditorPrefs.GetBool("gitLocksColorblindMode");
            greenLockIcon = LoadIcon(cb ? "greenLock_cb.png" : "greenLock.png");
        }
        return greenLockIcon;
    }

    private static Texture GetOrangeLockIcon(bool forceReload = false)
    {
        if (orangeLockIcon == null || forceReload)
        {
            bool cb = EditorPrefs.HasKey("gitLocksColorblindMode") && EditorPrefs.GetBool("gitLocksColorblindMode");
            orangeLockIcon = LoadIcon(cb ? "orangeLock_cb.png" : "orangeLock.png");
        }
        return orangeLockIcon;
    }

    private static Texture GetRedLockIcon(bool forceReload = false)
    {
        if (redLockIcon == null || forceReload)
        {
            bool cb = EditorPrefs.HasKey("gitLocksColorblindMode") && EditorPrefs.GetBool("gitLocksColorblindMode");
            redLockIcon = LoadIcon(cb ? "redLock_cb.png" : "redLock.png");
        }
        return redLockIcon;
    }

    private static Texture GetMixedLockIcon(bool forceReload = false)
    {
        if (mixedLockIcon == null || forceReload)
        {
            bool cb = EditorPrefs.HasKey("gitLocksColorblindMode") && EditorPrefs.GetBool("gitLocksColorblindMode");
            mixedLockIcon = LoadIcon(cb ? "mixedLock_cb.png" : "mixedLock.png");
        }
        return mixedLockIcon;
    }

    public static void RefreshLockIcons()
    {
        GetGreenLockIcon(true);
        GetOrangeLockIcon(true);
        GetRedLockIcon(true);
        GetMixedLockIcon(true);
    }

    // ------------------------
    // Git lock window features
    // ------------------------
    public void CreateGUI()
    {
        GitLocks.CheckLocksRefresh();

        VisualElement root = rootVisualElement;
        
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.tomduchene.unity-git-locks/Editor/UI/GitLocksWindow.uxml");
        if (visualTree == null)
            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/unity-git-locks-main/Editor/UI/GitLocksWindow.uxml");
        
        if (visualTree != null)
        {
            visualTree.CloneTree(root);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.tomduchene.unity-git-locks/Editor/UI/GitLocksWindow.uss");
            if (styleSheet == null)
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/unity-git-locks-main/Editor/UI/GitLocksWindow.uss");
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
        }
        else
        {
            root.Add(new Label("Failed to load UXML for GitLocksWindow. Please ensure UI folder is correct."));
            return;
        }

        disabledContainer = root.Q<VisualElement>("disabled-container");
        setupContainer = root.Q<VisualElement>("setup-container");
        mainToolContainer = root.Q<VisualElement>("main-tool-container");

        root.Q<Button>("enable-btn")?.RegisterCallback<ClickEvent>(evt => SettingsService.OpenUserPreferences("Preferences/Git Locks"));

        // Removed hardcoded GitHub button callback
        root.Q<Button>("setup-prefs-btn")?.RegisterCallback<ClickEvent>(evt => SettingsService.OpenUserPreferences("Preferences/Git Locks"));

        refreshTimeLabel = root.Q<Label>("refresh-time-label");
        root.Q<Button>("open-settings-btn")?.RegisterCallback<ClickEvent>(evt => SettingsService.OpenUserPreferences("Preferences/Git Locks"));
        root.Q<Button>("refresh-locks-btn")?.RegisterCallback<ClickEvent>(evt => GitLocks.RefreshLocks());

        var lockObjField = root.Q<UnityEditor.UIElements.ObjectField>("lock-object-field");
        if (lockObjField != null) lockObjField.objectType = typeof(UnityEngine.Object);
            
        root.Q<Button>("lock-asset-btn")?.RegisterCallback<ClickEvent>(evt => {
            if (lockObjField != null && lockObjField.value != null)
            {
                string path = AssetDatabase.GetAssetPath(lockObjField.value);
                if (string.IsNullOrEmpty(path))
                    path = GitLocks.GetAssetPathFromPrefabGameObject(lockObjField.value.GetInstanceID());
                
                GitLocks.LockFile(path);
                lockObjField.value = null;
                GitLocks.RefreshLocks();
            }
        });

        myLocksScrollView = root.Q<ScrollView>("my-locks-list");
        otherLocksScrollView = root.Q<ScrollView>("other-locks-list");

        var selectAllToggle = root.Q<Toggle>("select-all-toggle");
        if (selectAllToggle != null)
        {
            selectAllToggle.RegisterValueChangedCallback(evt => {
                if (selectedLocks == null) selectedLocks = new List<GitLocksObject>();
                if (evt.newValue)
                {
                    selectedLocks.Clear();
                    var myLocks = GitLocks.GetMyLocks();
                    if (myLocks != null) selectedLocks.AddRange(myLocks);
                }
                else
                {
                    selectedLocks.Clear();
                }
                RefreshUI(); // Refresh the whole tree to update toggles immediately
            });
        }

        root.Q<Button>("unlock-selected-btn")?.RegisterCallback<ClickEvent>(evt => {
            if (selectedLocks != null && selectedLocks.Count > 0)
            {
                GitLocks.UnlockMultipleLocks(selectedLocks);
                selectedLocks.Clear();
                if (selectAllToggle != null) selectAllToggle.SetValueWithoutNotify(false);
            }
        });

        RefreshUI();
        root.schedule.Execute(UpdateUIDynamically).Every(1000);
    }

    public void RefreshUI()
    {
        if (disabledContainer == null || setupContainer == null || mainToolContainer == null) return;

        if (!GitLocks.IsEnabled())
        {
            disabledContainer.style.display = DisplayStyle.Flex;
            setupContainer.style.display = DisplayStyle.None;
            mainToolContainer.style.display = DisplayStyle.None;
        }
        else if (string.IsNullOrEmpty(GitLocks.GetGitUsername()))
        {
            disabledContainer.style.display = DisplayStyle.None;
            setupContainer.style.display = DisplayStyle.Flex;
            mainToolContainer.style.display = DisplayStyle.None;
        }
        else
        {
            disabledContainer.style.display = DisplayStyle.None;
            setupContainer.style.display = DisplayStyle.None;
            mainToolContainer.style.display = DisplayStyle.Flex;

            UpdateUIDynamically();

            if (myLocksScrollView != null)
            {
                var myLocks = GitLocks.GetMyLocks() ?? new List<GitLocksObject>();
                BuildLockTree(myLocksScrollView, myLocks, true);
            }

            if (otherLocksScrollView != null)
            {
                var otherLocks = GitLocks.GetOtherLocks() ?? new List<GitLocksObject>();
                BuildLockTree(otherLocksScrollView, otherLocks, false);
            }
        }
    }

    private void UpdateUIDynamically()
    {
        if (refreshTimeLabel != null)
        {
            if (GitLocks.CurrentlyRefreshing)
                refreshTimeLabel.text = "Last refresh time : currently refreshing...";
            else
                refreshTimeLabel.text = "Last refresh time : " + GitLocks.LastRefresh.ToShortTimeString();
        }

        var autoRefreshLabel = rootVisualElement?.Q<Label>("auto-refresh-label");
        if (autoRefreshLabel != null)
        {
            bool autoRefresh = EditorPrefs.GetBool("gitLocksAutoRefreshLocks", true);
            if (autoRefresh)
            {
                int refreshLocksInterval = EditorPrefs.GetInt("gitLocksRefreshLocksInterval", 5);
                autoRefreshLabel.text = "Auto refresh every " + refreshLocksInterval + " " + (refreshLocksInterval > 1 ? "minutes" : "minute");
            }
            else
            {
                autoRefreshLabel.text = "Manual refresh only";
            }
        }
    }
}
