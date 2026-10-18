using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ASHES OF TIME — Main Menu Manager
/// Controls menu layout, button actions, settings, instructions, and cinematic Wake Up transition.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Cameras & Transition")]
    public Camera menuCamera;
    public GameObject fpsController;
    public Camera cabinCamera;
    [Tooltip("Duration of the camera transition from Menu to Cabin/Player")]
    public float transitionDuration = 2.8f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("UI Panels & Canvas")]
    public Canvas menuCanvas;
    public CanvasGroup menuCanvasGroup;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject instructionsPanel;

    [Header("Waking Up Eye Overlay")]
    public CanvasGroup wakingOverlayGroup;
    public RectTransform topEyelid;
    public RectTransform bottomEyelid;

    [Header("Menu Buttons")]
    public Button wakeUpButton;
    public Button settingsButton;
    public Button instructionsButton;
    public Button exitButton;

    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioClip switchClip; // switch_004.ogg

    [Header("Settings Controls")]
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    private bool isTransitioning = false;
    private Camera targetGameplayCamera;
    private CharController_Motor playerMotor;

    private const string PREF_MASTER_VOL = "AOT_MasterVolume";
    private const string PREF_SFX_VOL = "AOT_SFXVolume";
    private const string PREF_MUSIC_VOL = "AOT_MusicVolume";
    private const string PREF_SENSITIVITY = "AOT_Sensitivity";

    private void Awake()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        // 1. Initial scene state: menu camera active, player controller disabled
        if (menuCamera != null) menuCamera.gameObject.SetActive(true);
        if (fpsController != null)
        {
            fpsController.SetActive(false);
            playerMotor = fpsController.GetComponent<CharController_Motor>();
            Camera pCam = fpsController.GetComponentInChildren<Camera>(true);
            if (pCam != null) targetGameplayCamera = pCam;
        }

        if (cabinCamera != null)
        {
            cabinCamera.gameObject.SetActive(false);
        }

        // 2. Cursor unlocked for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Ensure UI panels are in default state
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }

        // 4. Eyelid overlay initially invisible
        if (wakingOverlayGroup != null)
        {
            wakingOverlayGroup.alpha = 0f;
            wakingOverlayGroup.blocksRaycasts = false;
        }

        // 5. Initialize Settings
        InitSettings();

        // 6. Hook up buttons
        if (wakeUpButton != null) wakeUpButton.onClick.AddListener(OnWakeUpClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (instructionsButton != null) instructionsButton.onClick.AddListener(OnInstructionsClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
    }

    private void InitSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1f);
        float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);
        float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.8f);
        float sens = PlayerPrefs.GetFloat(PREF_SENSITIVITY, playerMotor != null ? playerMotor.sensitivity : 45f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVol;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVol;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVol;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = sens;
            mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            if (sensitivityValueText != null) sensitivityValueText.text = Mathf.RoundToInt(sens).ToString();
        }

        AudioListener.volume = masterVol;
        if (playerMotor != null) playerMotor.sensitivity = sens;
    }

    public void PlaySound()
    {
        if (switchClip != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(switchClip, 1.0f);
        }
    }

    #region Menu Button Callbacks

    public void OnWakeUpClicked()
    {
        if (isTransitioning) return;

        PlaySound();
        StartCoroutine(WakeUpDirectRoutine());
    }

    // Legacy support for EscapeMenu / wakeupbutton.cs
    public void OnWakeUpPressed()
    {
        OnWakeUpClicked();
    }

    public void OnSettingsClicked()
    {
        if (isTransitioning) return;
        PlaySound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnInstructionsClicked()
    {
        if (isTransitioning) return;
        PlaySound();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(true);
    }

    public void OnBackFromSettingsClicked()
    {
        PlaySound();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OnBackFromInstructionsClicked()
    {
        PlaySound();
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OnExitClicked()
    {
        if (isTransitioning) return;
        PlaySound();
        Debug.Log("Exiting Ashes of Time...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Settings Handlers

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (uiAudioSource != null) uiAudioSource.volume = value;
        PlayerPrefs.SetFloat(PREF_SFX_VOL, value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, value);
    }

    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
    }

    public void OnFullscreenToggled(bool isFull)
    {
        Screen.fullScreen = isFull;
    }

    public void OnSensitivityChanged(float value)
    {
        if (playerMotor != null) playerMotor.sensitivity = value;
        if (sensitivityValueText != null) sensitivityValueText.text = Mathf.RoundToInt(value).ToString();
        PlayerPrefs.SetFloat(PREF_SENSITIVITY, value);
    }

    #endregion

    #region Wake Up Routine

    private IEnumerator WakeUpDirectRoutine()
    {
        isTransitioning = true;

        // 1. Disable menu interaction immediately
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        // 2. Smooth fade out of menu UI
        float fadeDuration = 0.25f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (menuCanvasGroup != null) menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0f;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);

        // 3. Deactivate menu cameras
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        if (cabinCamera != null) cabinCamera.gameObject.SetActive(false);

        // 4. Activate player right where he is — no moving backward!
        if (fpsController != null)
        {
            fpsController.SetActive(true);
            CharController_Motor motor = fpsController.GetComponent<CharController_Motor>();
            if (motor != null)
            {
                motor.SetCursorLock(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        isTransitioning = false;
    }

    private IEnumerator AnimateEyelids(float duration, float openAmount)
    {
        if (topEyelid == null || bottomEyelid == null) yield break;

        float startTop = topEyelid.anchoredPosition.y;
        float startBottom = bottomEyelid.anchoredPosition.y;

        // In 1080p, top eyelid is at 540 when closed, 1080 when open
        // bottom eyelid is at -540 when closed, -1080 when open
        float targetTopY = Mathf.Lerp(0f, 600f, openAmount);
        float targetBottomY = Mathf.Lerp(0f, -600f, openAmount);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            topEyelid.anchoredPosition = new Vector2(0f, Mathf.Lerp(startTop, targetTopY, smooth));
            bottomEyelid.anchoredPosition = new Vector2(0f, Mathf.Lerp(startBottom, targetBottomY, smooth));
            yield return null;
        }

        topEyelid.anchoredPosition = new Vector2(0f, targetTopY);
        bottomEyelid.anchoredPosition = new Vector2(0f, targetBottomY);
    }

    #endregion
}
