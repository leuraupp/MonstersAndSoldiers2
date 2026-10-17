using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Opções (GDD 8.4): música, efeitos e, só no desktop, resolução e tela cheia.
/// Salva ao fechar. Usado no menu principal e na pausa.
/// </summary>
public class OptionsPanel : MonoBehaviour {
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private GameObject screenSettings;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button backButton;

    private readonly List<string> resolutions = new List<string>();
    private Action onClosed;

    public bool IsOpen => panel.activeSelf;

    private void Awake() {
        panel.SetActive(false);
    }

    private void Start() {
        backButton.onClick.AddListener(Close);
    }

    public void Open(Action closed = null) {
        onClosed = closed;
        var options = OptionsService.Options;
        musicSlider.SetValueWithoutNotify(options.MusicVolume);
        sfxSlider.SetValueWithoutNotify(options.SfxVolume);

        bool screen = OptionsService.SupportsScreenSettings;
        if (screenSettings != null) screenSettings.SetActive(screen);
        if (screen) FillResolutions(options.Resolution);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(options.Fullscreen);

        panel.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
    }

    public void Close() {
        if (!panel.activeSelf) return;
        var options = OptionsService.Options;
        options.MusicVolume = musicSlider.value;
        options.SfxVolume = sfxSlider.value;
        if (OptionsService.SupportsScreenSettings) {
            if (fullscreenToggle != null) options.Fullscreen = fullscreenToggle.isOn;
            if (resolutionDropdown != null && resolutionDropdown.value < resolutions.Count) {
                options.Resolution = resolutions[resolutionDropdown.value];
            }
        }
        OptionsService.Save();

        panel.SetActive(false);
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private void FillResolutions(string current) {
        if (resolutionDropdown == null) return;
        resolutions.Clear();
        resolutions.AddRange(Screen.resolutions
            .Select(r => $"{r.width}x{r.height}")
            .Distinct());
        if (resolutions.Count == 0) resolutions.Add($"{Screen.width}x{Screen.height}");

        string selected = string.IsNullOrEmpty(current) ? $"{Screen.width}x{Screen.height}" : current;
        if (!resolutions.Contains(selected)) resolutions.Add(selected);

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutions);
        resolutionDropdown.SetValueWithoutNotify(resolutions.IndexOf(selected));
    }
}
