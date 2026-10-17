using MonstersAndSoldiers.Core;
using UnityEngine;

/// <summary>IKeyValueStore sobre o PlayerPrefs (no WebGL vira IndexedDB do navegador).</summary>
public class PlayerPrefsStore : IKeyValueStore {
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public void Save() => PlayerPrefs.Save();
}
