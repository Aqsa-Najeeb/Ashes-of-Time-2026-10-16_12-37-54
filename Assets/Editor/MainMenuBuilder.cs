using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class MainMenuBuilder
{
    static MainMenuBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            try
            {
                BuildMenu();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error building menu: " + ex);
            }
        };
    }
    private const string SCENE_PATH = "Assets/Flooded_Grounds/Scenes/Scene_A.unity";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/Cinzel-Black SDF.asset";
    private const string AUDIO_PATH = "Assets/Vefects/Free Fire VFX URP/Audio/switch_004.ogg";

    // Sprites
    private const string SPRITE_FIRE_NORMAL = "Assets/Laterns and candles/Sprites/Molten_Fire_Emblem_Status_Bar-removebg-preview.png";
    private const string SPRITE_FIRE_HOVER = "Assets/Laterns and candles/Sprites/Molten_Flame_Hover_Bar-removebg-preview.png";
    private const string SPRITE_TIME_NORMAL = "Assets/Laterns and candles/Sprites/Golden_Steampunk_Clock_HUD_Banner-removebg-preview.png";
    private const string SPRITE_TIME_HOVER = "Assets/Laterns and candles/Sprites/Golden_Steampunk_Clock_HUD_Banner-hover.png";
    private const string SPRITE_SHADOW_NORMAL = "Assets/Laterns and candles/Sprites/Ornate_Shadowstone_Fantasy_HUD_Banner-removebg-preview.png";
    private const string SPRITE_SHADOW_HOVER = "Assets/Laterns and candles/Sprites/Molten_Shadow_HUD_Banner-removebg-preview.png";
    private const string SPRITE_EXIT_NORMAL = "Assets/Laterns and candles/Sprites/a_clean_high_resolution_game_ui_asset_on_a_transp__1_-removebg-preview.png";
    private const string SPRITE_EXIT_HOVER = "Assets/Laterns and candles/Sprites/a_clean_high_resolution_game_ui_asset-hover.png";

    [MenuItem("Tools/Build Ashes of Time Main Menu")]
    public static void BuildMenu()
    {
        Debug.Log("===> Starting Ashes of Time Main Menu Build...");

        // Ensure active scene is Scene_A
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != SCENE_PATH)
        {
            currentScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        TMP_FontAsset cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (cinzelFont == null)
        {
            Debug.LogError("Could not find Cinzel-Black SDF at " + FONT_PATH);
            return;
        }

        AudioClip switchClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_PATH);
        if (switchClip == null)
        {
            Debug.LogWarning("Could not find switch_004.ogg at " + AUDIO_PATH);
        }

        // Load Sprites
        Sprite fireNormal = LoadSprite(SPRITE_FIRE_NORMAL);
        Sprite fireHover = LoadSprite(SPRITE_FIRE_HOVER);
        Sprite timeNormal = LoadSprite(SPRITE_TIME_NORMAL);
        Sprite timeHover = LoadSprite(SPRITE_TIME_HOVER) ?? timeNormal;
        Sprite shadowNormal = LoadSprite(SPRITE_SHADOW_NORMAL);
        Sprite shadowHover = LoadSprite(SPRITE_SHADOW_HOVER);
        Sprite exitNormal = LoadSprite(SPRITE_EXIT_NORMAL);
        Sprite exitHover = LoadSprite(SPRITE_EXIT_HOVER) ?? exitNormal;

        // 1. Locate FpsController
        GameObject fpsController = GameObject.Find("FpsController");
        if (fpsController == null)
        {
            Debug.LogError("FpsController GameObject not found in scene!");
        }

        // 2. Setup or Find MenuCamera
        GameObject menuCamObj = GameObject.Find("MenuCamera");
        if (menuCamObj == null)
        {
            menuCamObj = new GameObject("MenuCamera");
            Undo.RegisterCreatedObjectUndo(menuCamObj, "Create MenuCamera");
        }

        // Vantage point framing the mystical Fire, Shadow & Time Altar Sanctuary
        menuCamObj.transform.position = new Vector3(456.0f, 29.8f, 163.0f);
        menuCamObj.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);

        Camera menuCam = menuCamObj.GetComponent<Camera>();
        if (menuCam == null) menuCam = menuCamObj.AddComponent<Camera>();
        menuCam.clearFlags = CameraClearFlags.Skybox;
        menuCam.fieldOfView = 60f;
        menuCam.nearClipPlane = 0.1f;
        menuCam.farClipPlane = 1000f;
        menuCam.depth = 1;
        menuCam.gameObject.SetActive(true);

        AudioListener menuListener = menuCamObj.GetComponent<AudioListener>();
        if (menuListener == null) menuListener = menuCamObj.AddComponent<AudioListener>();
        menuListener.enabled = true;

        // Ensure UniversalAdditionalCameraData is present for URP
        var camDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (camDataType != null && menuCamObj.GetComponent(camDataType) == null)
        {
            menuCamObj.AddComponent(camDataType);
        }

        // 3. Setup CabinCamera (destination / focus camera for waking perspective)
        GameObject cabinCamObj = GameObject.Find("CabinCamera");
        if (cabinCamObj == null)
        {
            cabinCamObj = new GameObject("CabinCamera");
            Undo.RegisterCreatedObjectUndo(cabinCamObj, "Create CabinCamera");
        }
        if (fpsController != null)
        {
            cabinCamObj.transform.position = fpsController.transform.position + new Vector3(0f, 1.6f, 0f);
            cabinCamObj.transform.rotation = fpsController.transform.rotation;
        }
        else
        {
            cabinCamObj.transform.position = new Vector3(456.03f, 28.99f + 1.6f, 153.83f);
            cabinCamObj.transform.rotation = Quaternion.identity;
        }
        Camera cabinCam = cabinCamObj.GetComponent<Camera>();
        if (cabinCam == null) cabinCam = cabinCamObj.AddComponent<Camera>();
        cabinCam.fieldOfView = 60f;
        cabinCam.depth = 0;
        cabinCam.gameObject.SetActive(false);

        // 4. Ensure EventSystem exists
        EnsureEventSystem();

        // 5. Setup Canvas
        GameObject canvasObj = GameObject.Find("MainMenu_Canvas");
        if (canvasObj != null)
        {
            Undo.DestroyObjectImmediate(canvasObj);
        }

        canvasObj = new GameObject("MainMenu_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create MainMenu_Canvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                          AdditionalCanvasShaderChannels.Normal |
                                          AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        MainMenuManager manager = canvasObj.AddComponent<MainMenuManager>();
        manager.menuCanvas = canvas;
        manager.menuCanvasGroup = canvasGroup;
        manager.menuCamera = menuCam;
        manager.fpsController = fpsController;
        manager.cabinCamera = cabinCam;
        manager.switchClip = switchClip;
        manager.transitionDuration = 2.6f;

        // UI AudioSource
        AudioSource audioSource = canvasObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        manager.uiAudioSource = audioSource;

        // 6. Build Main Menu Panel
        GameObject mainPanel = CreateUIObject("MainMenu_Panel", canvasObj.transform);
        RectTransform mainPanelRT = mainPanel.GetComponent<RectTransform>();
        StretchFull(mainPanelRT);
        manager.mainMenuPanel = mainPanel;

        // Title Container
        GameObject titleContainer = CreateUIObject("Title_Container", mainPanel.transform);
        RectTransform titleRT = titleContainer.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0f, 310f);
        titleRT.sizeDelta = new Vector2(1300f, 130f);

        // Title Text: "ASHES OF TIME"
        GameObject titleTextObj = CreateUIObject("Title_Text", titleContainer.transform);
        RectTransform titleTextRT = titleTextObj.GetComponent<RectTransform>();
        StretchFull(titleTextRT);

        TextMeshProUGUI titleTMP = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleTMP.font = cinzelFont;
        titleTMP.text = "ASHES OF TIME";
        titleTMP.fontSize = 76;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 10f;
        titleTMP.enableVertexGradient = true;
        titleTMP.colorGradient = new VertexGradient(
            new Color(1.0f, 0.94f, 0.76f, 1f), // Top Left - Warm Light Gold
            new Color(1.0f, 0.94f, 0.76f, 1f), // Top Right
            new Color(0.92f, 0.58f, 0.18f, 1f), // Bottom Left - Fiery Amber
            new Color(0.85f, 0.45f, 0.12f, 1f)  // Bottom Right
        );
        titleTMP.outlineWidth = 0.12f;
        titleTMP.outlineColor = new Color32(28, 12, 4, 255);
        titleTMP.extraPadding = true;

        // Subtitle Text: "A DARK FANTASY OF TIME, SHADOW AND FIRE"
        GameObject subTextObj = CreateUIObject("Subtitle_Text", titleContainer.transform);
        RectTransform subTextRT = subTextObj.GetComponent<RectTransform>();
        subTextRT.anchorMin = new Vector2(0f, 0f);
        subTextRT.anchorMax = new Vector2(1f, 0f);
        subTextRT.pivot = new Vector2(0.5f, 0f);
        subTextRT.anchoredPosition = new Vector2(0f, -22f);
        subTextRT.sizeDelta = new Vector2(0f, 28f);

        TextMeshProUGUI subTMP = subTextObj.AddComponent<TextMeshProUGUI>();
        subTMP.font = cinzelFont;
        subTMP.text = "— OF TIME, SHADOW AND FIRE —";
        subTMP.fontSize = 17;
        subTMP.fontStyle = FontStyles.Normal;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.characterSpacing = 16f;
        subTMP.color = new Color(0.85f, 0.72f, 0.55f, 0.75f);

        // Buttons Container
        GameObject buttonsContainer = CreateUIObject("Buttons_Container", mainPanel.transform);
        RectTransform buttonsRT = buttonsContainer.GetComponent<RectTransform>();
        buttonsRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRT.pivot = new Vector2(0.5f, 0.5f);
        buttonsRT.anchoredPosition = new Vector2(0f, -60f);
        buttonsRT.sizeDelta = new Vector2(350f, 540f);

        // Button dimensions (preserving 701x356 = 1.969:1 aspect ratio)
        Vector2 btnSize = new Vector2(340f, 172.6f);

        // Button 1: Wake Up (Fire Theme)
        Button btnWakeUp = CreateMenuButton(
            "Button_WakeUp",
            buttonsContainer.transform,
            new Vector2(0f, 180f),
            btnSize,
            fireNormal,
            fireHover,
            "WAKE UP",
            cinzelFont,
            new Color(1f, 0.88f, 0.65f),
            switchClip,
            audioSource
        );
        manager.wakeUpButton = btnWakeUp;

        // Button 2: Settings (Time Theme)
        Button btnSettings = CreateMenuButton(
            "Button_Settings",
            buttonsContainer.transform,
            new Vector2(0f, 60f),
            btnSize,
            timeNormal,
            timeHover,
            "SETTINGS",
            cinzelFont,
            new Color(1f, 0.88f, 0.65f),
            switchClip,
            audioSource
        );
        manager.settingsButton = btnSettings;

        // Button 3: Instructions (Shadow Theme)
        Button btnInstructions = CreateMenuButton(
            "Button_Instructions",
            buttonsContainer.transform,
            new Vector2(0f, -60f),
            btnSize,
            shadowNormal,
            shadowHover,
            "INSTRUCTIONS",
            cinzelFont,
            new Color(1f, 0.88f, 0.65f),
            switchClip,
            audioSource
        );
        manager.instructionsButton = btnInstructions;

        // Button 4: Exit
        Button btnExit = CreateMenuButton(
            "Button_Exit",
            buttonsContainer.transform,
            new Vector2(0f, -180f),
            btnSize,
            exitNormal,
            exitHover,
            "EXIT",
            cinzelFont,
            new Color(1f, 0.88f, 0.65f),
            switchClip,
            audioSource
        );
        manager.exitButton = btnExit;

        // 7. Build Settings Panel
        GameObject settingsPanel = BuildSettingsPanel(canvasObj.transform, cinzelFont, switchClip, audioSource, manager);
        manager.settingsPanel = settingsPanel;
        settingsPanel.SetActive(false);

        // 8. Build Instructions Panel
        GameObject instructionsPanel = BuildInstructionsPanel(canvasObj.transform, cinzelFont, switchClip, audioSource, manager);
        manager.instructionsPanel = instructionsPanel;
        instructionsPanel.SetActive(false);

        // 9. Build Waking Overlay (Eyelids)
        BuildWakingOverlay(canvasObj.transform, manager);

        // Mark scene dirty and save
        EditorUtility.SetDirty(canvasObj);
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("===> Main Menu Build COMPLETE and SAVED successfully!");
    }

    private static Sprite LoadSprite(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            if (a is Sprite s) return s;
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.layer = 5; // UI layer
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Button CreateMenuButton(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Sprite normalSprite,
        Sprite hoverSprite,
        string labelText,
        TMP_FontAsset font,
        Color textColor,
        AudioClip clickClip,
        AudioSource audioSource)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = normalSprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        if (hoverSprite != null && hoverSprite != normalSprite)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = hoverSprite;
            ss.pressedSprite = hoverSprite;
            ss.selectedSprite = hoverSprite;
            btn.spriteState = ss;
        }
        else
        {
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.15f, 1.0f, 1f);
            cb.pressedColor = new Color(0.85f, 0.8f, 0.7f, 1f);
            btn.colors = cb;
        }

        // Add Button Effects (sound + smooth scale)
        MenuButtonEffects effects = btnObj.AddComponent<MenuButtonEffects>();
        effects.clickSound = clickClip;
        effects.audioSource = audioSource;
        effects.hoverScale = 1.035f;

        // Button Text label (offset to the right banner area so it doesn't overlap the left emblem)
        GameObject labelObj = CreateUIObject("Text_Label", btnObj.transform);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.32f, 0.20f);
        labelRT.anchorMax = new Vector2(0.95f, 0.80f);
        labelRT.pivot = new Vector2(0.5f, 0.5f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = labelText;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 8f;
        tmp.color = textColor;
        tmp.enableVertexGradient = true;
        tmp.colorGradient = new VertexGradient(
            new Color(1f, 0.98f, 0.88f),
            new Color(1f, 0.98f, 0.88f),
            new Color(0.92f, 0.75f, 0.40f),
            new Color(0.85f, 0.65f, 0.30f)
        );
        tmp.outlineWidth = 0.10f;
        tmp.outlineColor = new Color32(20, 10, 4, 255);
        tmp.raycastTarget = false;

        return btn;
    }

    private static GameObject BuildSettingsPanel(Transform parent, TMP_FontAsset font, AudioClip clickClip, AudioSource audioSource, MainMenuManager manager)
    {
        GameObject panelObj = CreateUIObject("Settings_Panel", parent);
        StretchFull(panelObj.GetComponent<RectTransform>());

        // Dim background
        Image dim = panelObj.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.03f, 0.85f);

        // Modal Box
        GameObject modalObj = CreateUIObject("Settings_Modal", panelObj.transform);
        RectTransform modalRT = modalObj.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(760f, 620f);

        Image modalImg = modalObj.AddComponent<Image>();
        modalImg.color = new Color(0.10f, 0.08f, 0.07f, 0.95f);

        // Modal Title
        GameObject titleObj = CreateUIObject("Title", modalObj.transform);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -30f);
        titleRT.sizeDelta = new Vector2(600f, 50f);

        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.font = font;
        titleTMP.text = "SETTINGS";
        titleTMP.fontSize = 38;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 8f;
        titleTMP.color = new Color(1f, 0.88f, 0.65f);

        // Divider
        CreateDivider(modalObj.transform, new Vector2(0f, 210f), 660f);

        // Rows
        float startY = 160f;
        float rowSpacing = 62f;

        manager.masterVolumeSlider = CreateSliderRow("Master Volume", modalObj.transform, new Vector2(0f, startY - 0 * rowSpacing), font);
        manager.sfxVolumeSlider = CreateSliderRow("SFX Volume", modalObj.transform, new Vector2(0f, startY - 1 * rowSpacing), font);
        manager.musicVolumeSlider = CreateSliderRow("Music Volume", modalObj.transform, new Vector2(0f, startY - 2 * rowSpacing), font);
        manager.qualityDropdown = CreateDropdownRow("Quality Level", modalObj.transform, new Vector2(0f, startY - 3 * rowSpacing), font);
        manager.fullscreenToggle = CreateToggleRow("Fullscreen", modalObj.transform, new Vector2(0f, startY - 4 * rowSpacing), font);
        manager.mouseSensitivitySlider = CreateSliderRow("Look Sensitivity", modalObj.transform, new Vector2(0f, startY - 5 * rowSpacing), font, 10f, 100f);

        // Back Button
        GameObject backBtnObj = CreateUIObject("Button_Back", modalObj.transform);
        RectTransform backRT = backBtnObj.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f);
        backRT.anchorMax = new Vector2(0.5f, 0f);
        backRT.pivot = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0f, 35f);
        backRT.sizeDelta = new Vector2(240f, 55f);

        Image backImg = backBtnObj.AddComponent<Image>();
        backImg.color = new Color(0.24f, 0.16f, 0.10f, 1f);

        Button backBtn = backBtnObj.AddComponent<Button>();
        ColorBlock cb = backBtn.colors;
        cb.normalColor = new Color(0.24f, 0.16f, 0.10f, 1f);
        cb.highlightedColor = new Color(0.40f, 0.28f, 0.16f, 1f);
        cb.pressedColor = new Color(0.18f, 0.12f, 0.08f, 1f);
        backBtn.colors = cb;

        MenuButtonEffects effects = backBtnObj.AddComponent<MenuButtonEffects>();
        effects.clickSound = clickClip;
        effects.audioSource = audioSource;

        backBtn.onClick.AddListener(manager.OnBackFromSettingsClicked);

        GameObject backTextObj = CreateUIObject("Text", backBtnObj.transform);
        StretchFull(backTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI backTMP = backTextObj.AddComponent<TextMeshProUGUI>();
        backTMP.font = font;
        backTMP.text = "BACK";
        backTMP.fontSize = 22;
        backTMP.fontStyle = FontStyles.Bold;
        backTMP.alignment = TextAlignmentOptions.Center;
        backTMP.characterSpacing = 6f;
        backTMP.color = new Color(1f, 0.88f, 0.65f);

        return panelObj;
    }

    private static GameObject BuildInstructionsPanel(Transform parent, TMP_FontAsset font, AudioClip clickClip, AudioSource audioSource, MainMenuManager manager)
    {
        GameObject panelObj = CreateUIObject("Instructions_Panel", parent);
        StretchFull(panelObj.GetComponent<RectTransform>());

        Image dim = panelObj.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.03f, 0.85f);

        GameObject modalObj = CreateUIObject("Instructions_Modal", panelObj.transform);
        RectTransform modalRT = modalObj.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(920f, 680f);

        Image modalImg = modalObj.AddComponent<Image>();
        modalImg.color = new Color(0.10f, 0.08f, 0.07f, 0.95f);

        // Title
        GameObject titleObj = CreateUIObject("Title", modalObj.transform);
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -28f);
        titleRT.sizeDelta = new Vector2(750f, 45f);

        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.font = font;
        titleTMP.text = "INSTRUCTIONS & LORE";
        titleTMP.fontSize = 34;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 6f;
        titleTMP.color = new Color(1f, 0.88f, 0.65f);

        CreateDivider(modalObj.transform, new Vector2(0f, 240f), 840f);

        // Content Area
        GameObject contentObj = CreateUIObject("Content", modalObj.transform);
        RectTransform contentRT = contentObj.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 0.5f);
        contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentRT.pivot = new Vector2(0.5f, 0.5f);
        contentRT.anchoredPosition = new Vector2(0f, 15f);
        contentRT.sizeDelta = new Vector2(840f, 440f);

        // Controls Card Box
        GameObject controlsBox = CreateUIObject("ControlsCard", contentObj.transform);
        RectTransform controlsRT = controlsBox.GetComponent<RectTransform>();
        controlsRT.anchorMin = new Vector2(0f, 0.36f);
        controlsRT.anchorMax = new Vector2(1f, 1f);
        controlsRT.offsetMin = Vector2.zero;
        controlsRT.offsetMax = Vector2.zero;

        Image controlsCardBg = controlsBox.AddComponent<Image>();
        controlsCardBg.color = new Color(0.14f, 0.11f, 0.09f, 0.80f);

        GameObject controlsTextObj = CreateUIObject("ControlsText", controlsBox.transform);
        RectTransform controlsTextRT = controlsTextObj.GetComponent<RectTransform>();
        controlsTextRT.anchorMin = Vector2.zero;
        controlsTextRT.anchorMax = Vector2.one;
        controlsTextRT.offsetMin = new Vector2(20f, 10f);
        controlsTextRT.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI controlsTMP = controlsTextObj.AddComponent<TextMeshProUGUI>();
        controlsTMP.font = font;
        controlsTMP.fontSize = 14;
        controlsTMP.lineSpacing = 0f;
        controlsTMP.textWrappingMode = TextWrappingModes.Normal;
        controlsTMP.color = new Color(0.92f, 0.88f, 0.82f);
        controlsTMP.text =
            "<b><color=#F5C26B>SURVIVAL CONTROLS</color></b>\n" +
            "  •  <b>W, A, S, D / Arrow Keys</b>  —  Move Character\n" +
            "  •  <b>Left Shift</b>  —  Sprint / Accelerate\n" +
            "  •  <b>Spacebar</b>  —  Jump / Surface Water\n" +
            "  •  <b>Left Ctrl / C</b>  —  Crouch / Dive Water\n" +
            "  •  <b>F Key</b>  —  Toggle Flashlight\n" +
            "  •  <b>E Key</b>  —  Grab / Pick Up Relics & Objects\n" +
            "  •  <b>Left Mouse Button</b>  —  Throw Held Object\n" +
            "  •  <b>Right Mouse / G</b>  —  Drop Held Object\n" +
            "  •  <b>Mouse Look</b>  —  Aim & Camera View\n" +
            "  •  <b>Escape</b>  —  Unlock Mouse Cursor / Pause";

        // Lore Card Box
        GameObject loreBox = CreateUIObject("LoreCard", contentObj.transform);
        RectTransform loreRT = loreBox.GetComponent<RectTransform>();
        loreRT.anchorMin = new Vector2(0f, 0f);
        loreRT.anchorMax = new Vector2(1f, 0.33f);
        loreRT.offsetMin = Vector2.zero;
        loreRT.offsetMax = Vector2.zero;

        Image loreCardBg = loreBox.AddComponent<Image>();
        loreCardBg.color = new Color(0.14f, 0.11f, 0.09f, 0.80f);

        GameObject loreTextObj = CreateUIObject("LoreText", loreBox.transform);
        RectTransform loreTextRT = loreTextObj.GetComponent<RectTransform>();
        loreTextRT.anchorMin = Vector2.zero;
        loreTextRT.anchorMax = Vector2.one;
        loreTextRT.offsetMin = new Vector2(20f, 10f);
        loreTextRT.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI loreTMP = loreTextObj.AddComponent<TextMeshProUGUI>();
        loreTMP.font = font;
        loreTMP.fontSize = 13.5f;
        loreTMP.lineSpacing = 0f;
        loreTMP.textWrappingMode = TextWrappingModes.Normal;
        loreTMP.color = new Color(0.85f, 0.78f, 0.65f, 0.95f);
        loreTMP.text =
            "<b><color=#F5C26B>THE DOMAIN OF ASHES</color></b>\n" +
            "Awaken from your deep slumber within the sunken sanctuary. Bound by the primal forces of Fire, Shadow, and Time, " +
            "explore the flooded grounds to reclaim sacred relics. Tread cautiously where darkness gathers.";

        // Back Button
        GameObject backBtnObj = CreateUIObject("Button_Back", modalObj.transform);
        RectTransform backRT = backBtnObj.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f);
        backRT.anchorMax = new Vector2(0.5f, 0f);
        backRT.pivot = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0f, 32f);
        backRT.sizeDelta = new Vector2(220f, 50f);

        Image backImg = backBtnObj.AddComponent<Image>();
        backImg.color = new Color(0.24f, 0.16f, 0.10f, 1f);

        Button backBtn = backBtnObj.AddComponent<Button>();
        ColorBlock cb = backBtn.colors;
        cb.normalColor = new Color(0.24f, 0.16f, 0.10f, 1f);
        cb.highlightedColor = new Color(0.40f, 0.28f, 0.16f, 1f);
        cb.pressedColor = new Color(0.18f, 0.12f, 0.08f, 1f);
        backBtn.colors = cb;

        MenuButtonEffects effects = backBtnObj.AddComponent<MenuButtonEffects>();
        effects.clickSound = clickClip;
        effects.audioSource = audioSource;

        backBtn.onClick.AddListener(manager.OnBackFromInstructionsClicked);

        GameObject backTextObj = CreateUIObject("Text", backBtnObj.transform);
        StretchFull(backTextObj.GetComponent<RectTransform>());
        TextMeshProUGUI backTMP = backTextObj.AddComponent<TextMeshProUGUI>();
        backTMP.font = font;
        backTMP.text = "BACK";
        backTMP.fontSize = 22;
        backTMP.fontStyle = FontStyles.Bold;
        backTMP.alignment = TextAlignmentOptions.Center;
        backTMP.characterSpacing = 6f;
        backTMP.color = new Color(1f, 0.88f, 0.65f);

        return panelObj;
    }

    private static void BuildWakingOverlay(Transform parent, MainMenuManager manager)
    {
        GameObject overlayObj = CreateUIObject("Waking_Overlay", parent);
        StretchFull(overlayObj.GetComponent<RectTransform>());

        CanvasGroup cg = overlayObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        manager.wakingOverlayGroup = cg;

        // Top Eyelid
        GameObject topObj = CreateUIObject("TopEyelid", overlayObj.transform);
        RectTransform topRT = topObj.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 0.5f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 0f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0f, 200f);

        Image topImg = topObj.AddComponent<Image>();
        topImg.color = Color.black;
        manager.topEyelid = topRT;

        // Bottom Eyelid
        GameObject bottomObj = CreateUIObject("BottomEyelid", overlayObj.transform);
        RectTransform bottomRT = bottomObj.GetComponent<RectTransform>();
        bottomRT.anchorMin = new Vector2(0f, 0f);
        bottomRT.anchorMax = new Vector2(1f, 0.5f);
        bottomRT.pivot = new Vector2(0.5f, 1f);
        bottomRT.anchoredPosition = Vector2.zero;
        bottomRT.sizeDelta = new Vector2(0f, 200f);

        Image bottomImg = bottomObj.AddComponent<Image>();
        bottomImg.color = Color.black;
        manager.bottomEyelid = bottomRT;
    }

    private static void CreateDivider(Transform parent, Vector2 pos, float width)
    {
        GameObject divObj = CreateUIObject("Divider", parent);
        RectTransform divRT = divObj.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 0.5f);
        divRT.anchorMax = new Vector2(0.5f, 0.5f);
        divRT.pivot = new Vector2(0.5f, 0.5f);
        divRT.anchoredPosition = pos;
        divRT.sizeDelta = new Vector2(width, 2f);

        Image divImg = divObj.AddComponent<Image>();
        divImg.color = new Color(0.85f, 0.65f, 0.35f, 0.45f);
    }

    private static Slider CreateSliderRow(string labelText, Transform parent, Vector2 pos, TMP_FontAsset font, float minVal = 0f, float maxVal = 1f)
    {
        GameObject rowObj = CreateUIObject("Row_" + labelText.Replace(" ", ""), parent);
        RectTransform rowRT = rowObj.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = pos;
        rowRT.sizeDelta = new Vector2(660f, 40f);

        // Label
        GameObject labelObj = CreateUIObject("Label", rowObj.transform);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.45f, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.font = font;
        labelTMP.text = labelText;
        labelTMP.fontSize = 20;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.color = new Color(0.9f, 0.85f, 0.78f);

        // Slider
        GameObject sliderObj = CreateUIObject("Slider", rowObj.transform);
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.48f, 0.15f);
        sliderRT.anchorMax = new Vector2(1f, 0.85f);
        sliderRT.offsetMin = Vector2.zero;
        sliderRT.offsetMax = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minVal;
        slider.maxValue = maxVal;
        slider.value = (minVal + maxVal) * 0.5f;

        // Background
        GameObject bgObj = CreateUIObject("Background", sliderObj.transform);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        StretchFull(bgRT);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.14f, 0.11f, 1f);

        // Fill Area
        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5f, 0f);
        fillAreaRT.offsetMax = new Vector2(-5f, 0f);

        GameObject fillObj = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.92f, 0.65f, 0.25f, 1f);
        slider.fillRect = fillRT;

        // Handle Area
        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        StretchFull(handleAreaRT);
        handleAreaRT.offsetMin = new Vector2(10f, 0f);
        handleAreaRT.offsetMax = new Vector2(-10f, 0f);

        GameObject handleObj = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRT = handleObj.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 26f);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(1f, 0.90f, 0.65f, 1f);
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private static TMP_Dropdown CreateDropdownRow(string labelText, Transform parent, Vector2 pos, TMP_FontAsset font)
    {
        GameObject rowObj = CreateUIObject("Row_" + labelText.Replace(" ", ""), parent);
        RectTransform rowRT = rowObj.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = pos;
        rowRT.sizeDelta = new Vector2(660f, 40f);

        GameObject labelObj = CreateUIObject("Label", rowObj.transform);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.45f, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.font = font;
        labelTMP.text = labelText;
        labelTMP.fontSize = 20;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.color = new Color(0.9f, 0.85f, 0.78f);

        GameObject ddObj = CreateUIObject("Dropdown", rowObj.transform);
        RectTransform ddRT = ddObj.GetComponent<RectTransform>();
        ddRT.anchorMin = new Vector2(0.48f, 0.05f);
        ddRT.anchorMax = new Vector2(1f, 0.95f);
        ddRT.offsetMin = Vector2.zero;
        ddRT.offsetMax = Vector2.zero;

        Image ddImg = ddObj.AddComponent<Image>();
        ddImg.color = new Color(0.20f, 0.15f, 0.11f, 1f);

        TMP_Dropdown dropdown = ddObj.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = ddImg;

        // Label
        GameObject captionObj = CreateUIObject("Label", ddObj.transform);
        RectTransform capRT = captionObj.GetComponent<RectTransform>();
        StretchFull(capRT);
        capRT.offsetMin = new Vector2(15f, 0f);
        capRT.offsetMax = new Vector2(-25f, 0f);

        TextMeshProUGUI capTMP = captionObj.AddComponent<TextMeshProUGUI>();
        capTMP.font = font;
        capTMP.fontSize = 18;
        capTMP.alignment = TextAlignmentOptions.MidlineLeft;
        capTMP.color = new Color(1f, 0.92f, 0.75f);
        dropdown.captionText = capTMP;

        dropdown.ClearOptions();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Low"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Medium"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("High"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Ultra"));
        dropdown.RefreshShownValue();

        return dropdown;
    }

    private static Toggle CreateToggleRow(string labelText, Transform parent, Vector2 pos, TMP_FontAsset font)
    {
        GameObject rowObj = CreateUIObject("Row_" + labelText.Replace(" ", ""), parent);
        RectTransform rowRT = rowObj.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = pos;
        rowRT.sizeDelta = new Vector2(660f, 40f);

        GameObject labelObj = CreateUIObject("Label", rowObj.transform);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.45f, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.font = font;
        labelTMP.text = labelText;
        labelTMP.fontSize = 20;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.color = new Color(0.9f, 0.85f, 0.78f);

        GameObject toggleObj = CreateUIObject("Toggle", rowObj.transform);
        RectTransform toggleRT = toggleObj.GetComponent<RectTransform>();
        toggleRT.anchorMin = new Vector2(0.48f, 0.1f);
        toggleRT.anchorMax = new Vector2(0.48f, 0.1f);
        toggleRT.pivot = new Vector2(0f, 0f);
        toggleRT.anchoredPosition = Vector2.zero;
        toggleRT.sizeDelta = new Vector2(32f, 32f);

        Image bgImg = toggleObj.AddComponent<Image>();
        bgImg.color = new Color(0.20f, 0.15f, 0.11f, 1f);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;

        GameObject checkObj = CreateUIObject("Checkmark", toggleObj.transform);
        RectTransform checkRT = checkObj.GetComponent<RectTransform>();
        StretchFull(checkRT);
        checkRT.offsetMin = new Vector2(6f, 6f);
        checkRT.offsetMax = new Vector2(-6f, -6f);

        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0.95f, 0.70f, 0.28f, 1f);
        toggle.graphic = checkImg;

        return toggle;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }
    }
}
