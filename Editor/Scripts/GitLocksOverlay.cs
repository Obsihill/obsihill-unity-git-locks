using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Git Locks", true)]
public class GitLocksOverlay : Overlay
{
    private VisualElement root;
    private Label statusLabel;
    private Button actionBtn;
    private VisualElement iconEl;
    
    public override VisualElement CreatePanelContent()
    {
        root = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 5, paddingRight = 5, paddingTop = 2, paddingBottom = 2, backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f), borderBottomLeftRadius = 4, borderBottomRightRadius = 4, borderTopLeftRadius = 4, borderTopRightRadius = 4 } };

        iconEl = new VisualElement() { style = { width = 16, height = 16, marginRight = 5 } };
        root.Add(iconEl);

        statusLabel = new Label("No Selection");
        statusLabel.style.marginRight = 5;
        root.Add(statusLabel);

        actionBtn = new Button() { text = "Refresh" };
        actionBtn.clicked += OnActionClicked;
        root.Add(actionBtn);

        Selection.selectionChanged += UpdateOverlay;
        EditorApplication.update += PollRefresh;
        
        UpdateOverlay();

        return root;
    }

    private void PollRefresh()
    {
        if (root != null)
        {
            UpdateOverlay(); 
        }
    }

    private void OnActionClicked()
    {
        if (Selection.activeGameObject != null)
        {
            string path = GitLocks.GetAssetPathFromPrefabGameObject(Selection.activeGameObject);

            if (string.IsNullOrEmpty(path))
            {
                GitLocks.RefreshLocks();
                return;
            }

            if (GitLocks.IsObjectAvailableToUnlock(path))
            {
                GitLocks.UnlockFile(path);
                GitLocks.RefreshLocks();
            }
            else if (GitLocks.IsObjectAvailableToLock(path))
            {
                GitLocks.LockFile(path);
                GitLocks.RefreshLocks();
            }
            else
            {
                GitLocks.RefreshLocks();
            }
        }
        else
        {
            GitLocks.RefreshLocks();
        }
        UpdateOverlay();
    }

    public void UpdateOverlay()
    {
        if (root == null || !GitLocks.IsEnabled())
        {
            if (root != null) root.style.display = DisplayStyle.None;
            return;
        }
        
        root.style.display = DisplayStyle.Flex;

        var activeGo = Selection.activeGameObject;
        if (activeGo == null)
        {
            statusLabel.text = "No Scene GameObject Selected";
            iconEl.style.backgroundImage = null;
            actionBtn.text = "Refresh Locks";
            actionBtn.SetEnabled(true);
            return;
        }

        string path = GitLocks.GetAssetPathFromPrefabGameObject(activeGo);

        if (string.IsNullOrEmpty(path))
        {
            statusLabel.text = "Not a Prefab Asset";
            iconEl.style.backgroundImage = null;
            actionBtn.text = "Refresh Locks";
            actionBtn.SetEnabled(true);
            return;
        }

        if (GitLocks.CurrentlyRefreshing)
        {
            statusLabel.text = "Refreshing...";
            iconEl.style.backgroundImage = null;
            actionBtn.SetEnabled(false);
            return;
        }

        GitLocksObject lo = GitLocks.GetObjectInLockedCache(path);
        if (lo != null)
        {
            iconEl.style.backgroundColor = StyleKeyword.Null;
            iconEl.style.backgroundImage = (Texture2D)GitLocksDisplay.GetIconForLockedObject(lo);
            statusLabel.text = lo.IsMine() ? "Locked by you" : $"Locked by {lo.Owner.Name}";
            
            if (lo.IsMine())
            {
                actionBtn.text = "Unlock";
                actionBtn.SetEnabled(true);
            }
            else
            {
                actionBtn.text = "Locked";
                actionBtn.SetEnabled(EditorPrefs.GetBool("gitLocksShowForceButtons"));
                if (EditorPrefs.GetBool("gitLocksShowForceButtons")) actionBtn.text = "Force Unlock";
            }
        }
        else
        {
            iconEl.style.backgroundImage = null;
            if (!GitLocks.IsObjectAvailableToLock(path))
            {
                statusLabel.text = "Folder or Meta";
                actionBtn.text = "Refresh";
            }
            else
            {
                statusLabel.text = "Not Locked";
                actionBtn.text = "Lock";
            }
            actionBtn.SetEnabled(true);
        }
    }
}
