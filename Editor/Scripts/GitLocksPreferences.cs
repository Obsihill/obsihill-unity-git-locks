// <copyright file="GitLocksPreferences.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class GitLocksPreferences : SettingsProvider
{
    public GitLocksPreferences(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
    {
    }

    [SettingsProvider]
    public static SettingsProvider RegisterSettingsProvider()
    {
        var provider = new GitLocksPreferences("Preferences/Git Locks", SettingsScope.User);

        // Automatically extract all keywords from the Styles.
        provider.keywords = new string[]
        {
                                            "Enable Git LFS lock tool",
                                            "Git host username",
                                            "Auto refresh locks",
                                            "Refresh locks interval (minutes)",
                                            "Display locks conflict warning",
                                            "Warn if I still own locks on quit",
                                            "Minimum number of my locks to show"
        };
        return provider;
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        var container = new ScrollView();
        container.style.paddingLeft = 10;
        container.style.paddingRight = 10;
        container.style.paddingTop = 10;
        container.style.paddingBottom = 10;
        rootElement.Add(container);

        var header1 = new Label("General settings");
        header1.style.unityFontStyleAndWeight = FontStyle.Bold;
        container.Add(header1);
        
        var generalGroup = new VisualElement();
        generalGroup.style.marginLeft = 15;
        container.Add(generalGroup);

        var enableToggle = new Toggle("Enable Git LFS locks tool");
        enableToggle.value = EditorPrefs.GetBool("gitLocksEnabled", false);
        enableToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool("gitLocksEnabled", evt.newValue);
            GitLocksDisplay.RepaintAll(); // Trigger immediate update
        });
        generalGroup.Add(enableToggle);

        var usernameField = new TextField("Git host username");
        usernameField.value = EditorPrefs.GetString("gitLocksHostUsername", "");
        usernameField.RegisterValueChangedCallback(evt => EditorPrefs.SetString("gitLocksHostUsername", evt.newValue));
        generalGroup.Add(usernameField);

        var maxFilesField = new IntegerField("Max number of files grouped per request");
        maxFilesField.value = EditorPrefs.GetInt("gitLocksMaxFilesNumPerRequest", 15);
        maxFilesField.RegisterValueChangedCallback(evt => EditorPrefs.SetInt("gitLocksMaxFilesNumPerRequest", evt.newValue));
        generalGroup.Add(maxFilesField);

        var autoRefreshRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        var autoRefreshToggle = new Toggle("Auto refresh locks");
        autoRefreshToggle.value = EditorPrefs.GetBool("gitLocksAutoRefreshLocks", true);
        autoRefreshRow.Add(autoRefreshToggle);
        
        var intervalField = new IntegerField("every (minutes)");
        intervalField.value = EditorPrefs.GetInt("gitLocksRefreshLocksInterval", 5);
        intervalField.style.width = 150;
        intervalField.SetEnabled(autoRefreshToggle.value);
        autoRefreshRow.Add(intervalField);

        autoRefreshToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool("gitLocksAutoRefreshLocks", evt.newValue);
            intervalField.SetEnabled(evt.newValue);
        });
        intervalField.RegisterValueChangedCallback(evt => EditorPrefs.SetInt("gitLocksRefreshLocksInterval", evt.newValue));
        generalGroup.Add(autoRefreshRow);

        // Notifications
        var header2 = new Label("Notifications");
        header2.style.unityFontStyleAndWeight = FontStyle.Bold;
        header2.style.marginTop = 15;
        container.Add(header2);
        
        var notifGroup = new VisualElement { style = { marginLeft = 15 } };
        container.Add(notifGroup);

        var conflictToggle = new Toggle("Warn if a file I modified is already locked");
        conflictToggle.value = EditorPrefs.GetBool("displayLocksConflictWarning", true);
        conflictToggle.RegisterValueChangedCallback(evt => EditorPrefs.SetBool("displayLocksConflictWarning", evt.newValue));
        notifGroup.Add(conflictToggle);

        var warnQuitToggle = new Toggle("Warn if I still own locks on quit");
        warnQuitToggle.value = EditorPrefs.GetBool("warnIfIStillOwnLocksOnQuit", true);
        warnQuitToggle.RegisterValueChangedCallback(evt => EditorPrefs.SetBool("warnIfIStillOwnLocksOnQuit", evt.newValue));
        notifGroup.Add(warnQuitToggle);

        var modifiedServerToggle = new Toggle("Warn if the file has been modified on the server before locking (slower)");
        modifiedServerToggle.value = EditorPrefs.GetBool("warnIfFileHasBeenModifiedOnServer", true);
        notifGroup.Add(modifiedServerToggle);

        var branchesField = new TextField("Other branches to check (',' separated)");
        branchesField.value = EditorPrefs.GetString("gitLocksBranchesToCheck", "");
        branchesField.style.display = modifiedServerToggle.value ? DisplayStyle.Flex : DisplayStyle.None;
        branchesField.RegisterValueChangedCallback(evt => EditorPrefs.SetString("gitLocksBranchesToCheck", evt.newValue));
        notifGroup.Add(branchesField);

        modifiedServerToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool("warnIfFileHasBeenModifiedOnServer", evt.newValue);
            branchesField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        var notifyNewLocksToggle = new Toggle("Notify when there are new locks and when launching Unity");
        notifyNewLocksToggle.value = EditorPrefs.GetBool("notifyNewLocks", false);
        notifyNewLocksToggle.RegisterValueChangedCallback(evt => EditorPrefs.SetBool("notifyNewLocks", evt.newValue));
        notifGroup.Add(notifyNewLocksToggle);

        // Display
        var header3 = new Label("Display");
        header3.style.unityFontStyleAndWeight = FontStyle.Bold;
        header3.style.marginTop = 15;
        container.Add(header3);
        
        var descGroup = new VisualElement { style = { marginLeft = 15 } };
        container.Add(descGroup);

        var numOfLocksField = new IntegerField("Number of my locks displayed");
        numOfLocksField.value = EditorPrefs.GetInt("numOfMyLocksDisplayed", 5);
        numOfLocksField.RegisterValueChangedCallback(evt => EditorPrefs.SetInt("numOfMyLocksDisplayed", evt.newValue));
        descGroup.Add(numOfLocksField);

        var colorblindToggle = new Toggle("Colorblind mode");
        colorblindToggle.value = EditorPrefs.GetBool("gitLocksColorblindMode", false);
        colorblindToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool("gitLocksColorblindMode", evt.newValue);
            GitLocksDisplay.RefreshLockIcons();
            GitLocksDisplay.RepaintAll();
        });
        descGroup.Add(colorblindToggle);

        var debugToggle = new Toggle("Show debug logs");
        debugToggle.value = EditorPrefs.GetBool("gitLocksDebugMode", false);
        debugToggle.RegisterValueChangedCallback(evt => EditorPrefs.SetBool("gitLocksDebugMode", evt.newValue));
        descGroup.Add(debugToggle);

        var forceToggle = new Toggle("Show Force buttons");
        forceToggle.value = EditorPrefs.GetBool("gitLocksShowForceButtons", false);
        forceToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool("gitLocksShowForceButtons", evt.newValue);
            GitLocksDisplay.RepaintAll();
        });
        descGroup.Add(forceToggle);

        // Misc
        var header4 = new Label("Misc");
        header4.style.unityFontStyleAndWeight = FontStyle.Bold;
        header4.style.marginTop = 15;
        container.Add(header4);
        
        var miscGroup = new VisualElement { style = { marginLeft = 15 } };
        container.Add(miscGroup);

        var histBrowserRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        var histBrowserToggle = new Toggle("Show file history in browser");
        histBrowserToggle.value = EditorPrefs.GetBool("gitLocksShowHistoryInBrowser", false);
        histBrowserRow.Add(histBrowserToggle);

        var histUrlField = new TextField();
        histUrlField.value = EditorPrefs.GetString("gitLocksShowHistoryInBrowserUrl", "");
        histUrlField.style.width = 250;
        histUrlField.SetEnabled(histBrowserToggle.value);
        histBrowserRow.Add(histUrlField);

        histBrowserToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool("gitLocksShowHistoryInBrowser", evt.newValue);
            histUrlField.SetEnabled(evt.newValue);
        });
        histUrlField.RegisterValueChangedCallback(evt => EditorPrefs.SetString("gitLocksShowHistoryInBrowserUrl", evt.newValue));
        miscGroup.Add(histBrowserRow);

        // Git config
        var header5 = new Label("Git config and troubleshooting");
        header5.style.unityFontStyleAndWeight = FontStyle.Bold;
        header5.style.marginTop = 15;
        container.Add(header5);
        
        var gitGroup = new VisualElement { style = { marginLeft = 15 } };
        container.Add(gitGroup);

        var gitVerLabel = new Label(GitLocks.GetGitVersion());
        gitGroup.Add(gitVerLabel);

        if (GitLocks.IsGitOutdated())
        {
            var outdatedLabel = new Label("Your git version seems outdated (2.30.0 minimum), you may need to update it and then setup the Credentials Manager for the authentication to work properly");
            outdatedLabel.style.whiteSpace = WhiteSpace.Normal;
            gitGroup.Add(outdatedLabel);

            var updateBtn = new Button(() => GitLocks.ExecuteProcessTerminal("git", "update-git-for-windows", true)) { text = "Update Git for Windows" };
            gitGroup.Add(updateBtn);
        }

        var credBtn = new Button(() => GitLocks.ExecuteProcessTerminalWithConsole("git", "config --local credential.helper manager")) { text = "Setup credentials manager (when using HTTPS)" };
        gitGroup.Add(credBtn);
    }
}