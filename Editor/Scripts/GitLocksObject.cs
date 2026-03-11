// <copyright file="GitLocksObject.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using System;
using System.Runtime.Serialization;
using UnityEditor;
using JsonProperty = Unity.Plastic.Newtonsoft.Json.JsonPropertyAttribute;

[System.Serializable]
[DataContract]
public class GitLocksObject
{
    [DataMember(Name = "id")]
    [JsonProperty("id")]
    public int Id { get; set; }

    [DataMember(Name = "path")]
    [JsonProperty("path")]
    public string Path { get; set; }

    [DataMember(Name = "owner")]
    [JsonProperty("owner")]
    public LockedObjectOwner Owner { get; set; }

    [DataMember(Name = "locked_at")]
    [JsonProperty("locked_at")]
    public string LockedAt { get; set; }

    public UnityEngine.Object ObjectRef = null;

    public UnityEngine.Object GetObjectReference()
    {
        if (this.ObjectRef != null)
        {
            return this.ObjectRef;
        }
        else if (!string.IsNullOrEmpty(Path))
        {
            this.ObjectRef = AssetDatabase.LoadMainAssetAtPath(Path);
            return this.ObjectRef;
        }
        return null;
    }

    public bool IsMine()
    {
        if (Owner == null || string.IsNullOrEmpty(Owner.Name)) return false;
        return this.Owner.Name == GitLocks.GetGitUsername();
    }

    public string GetLockDateTimeString()
    {
        if (string.IsNullOrEmpty(LockedAt)) return "Unknown Date";
        
        if (DateTime.TryParse(LockedAt, out DateTime dt))
        {
            return dt.ToShortDateString() + " - " + dt.ToShortTimeString();
        }
        return LockedAt;
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        if (!string.IsNullOrEmpty(Path))
        {
            Path = FindRelativePath("Assets");
        }
    }

    private string FindRelativePath(string relativeFolder)
    {
        if (string.IsNullOrEmpty(Path)) return Path;
        int index = Path.IndexOf(relativeFolder, StringComparison.OrdinalIgnoreCase);
        if (index != -1) return Path.Substring(index);
        else return Path;
    }

}

[DataContract]
public class LockedObjectOwner
{
    [DataMember(Name = "name")]
    [JsonProperty("name")]
    public string Name;
}