using UnityEngine;

/// <summary>
/// Renders an authentic, stylish in-game HUD prompt and dynamic reticle.
/// Displays key prompts like '[E] Hold / Grab Lantern', active holding controls,
/// and smooth interactive reticle animations without any external sprite dependencies.
/// </summary>
public class GrabbableHUD : MonoBehaviour {
	[Header("HUD Settings")]
	[Tooltip("Show the minimalist center reticle.")]
	public bool showReticle = true;
	[Tooltip("Show the '[E] Grab / Hold' action tooltip.")]
	public bool showPrompt = true;
	[Tooltip("Prompt placement: True places it below crosshair, False places it bottom-center.")]
	public bool promptNearCrosshair = true;

	[Header("Styling")]
	public Color reticleNormalColor = new Color(1f, 1f, 1f, 0.65f);
	public Color reticleTargetColor = new Color(1.0f, 0.68f, 0.15f, 0.95f);
	public Color promptBadgeColor = new Color(0.95f, 0.65f, 0.15f, 1.0f);
	public Color promptBadgeTextColor = new Color(0.08f, 0.08f, 0.10f, 1.0f);
	public Color promptBgColor = new Color(0.07f, 0.09f, 0.13f, 0.88f);
	public Color promptBorderColor = new Color(1.0f, 0.65f, 0.15f, 0.45f);

	// Runtime references & state
	private PlayerGrabController grabController;
	private float promptAlpha = 0f;
	private float holdHudAlpha = 0f;
	private float reticleScale = 1f;
	private Texture2D whiteTex;
	private Texture2D pillBgTex;
	private Texture2D borderTex;
	private GUIStyle promptTextStyle;
	private GUIStyle keyBadgeStyle;
	private GUIStyle itemLabelStyle;
	private GUIStyle hintTextStyle;

	void Awake() {
		grabController = GetComponent<PlayerGrabController>();
		if (grabController == null) {
			grabController = GetComponentInParent<PlayerGrabController>();
		}
		GenerateProceduralTextures();
	}

	private void GenerateProceduralTextures() {
		if (whiteTex == null) {
			whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			whiteTex.SetPixel(0, 0, Color.white);
			whiteTex.Apply();
		}

		if (pillBgTex == null) {
			pillBgTex = CreateRoundedRectTexture(128, 48, 12, promptBgColor);
		}

		if (borderTex == null) {
			borderTex = CreateRoundedBorderTexture(128, 48, 12, 2, promptBorderColor, promptBgColor);
		}
	}

	private Texture2D CreateRoundedRectTexture(int width, int height, int radius, Color fillColor) {
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				float dist = DistanceToRoundedRect(x, y, width, height, radius);
				float alpha = Mathf.Clamp01(1.0f - dist);
				Color col = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * alpha);
				tex.SetPixel(x, y, col);
			}
		}
		tex.Apply();
		return tex;
	}

	private Texture2D CreateRoundedBorderTexture(int width, int height, int radius, int borderThickness, Color borderColor, Color innerColor) {
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				float dist = DistanceToRoundedRect(x, y, width, height, radius);
				float outerAlpha = Mathf.Clamp01(1.0f - dist);

				float innerDist = DistanceToRoundedRect(x, y, width, height, radius - borderThickness);
				float innerAlpha = Mathf.Clamp01(1.0f - (innerDist - borderThickness));

				Color finalCol;
				if (dist > 0.5f) {
					finalCol = Color.clear;
				} else if (innerDist > 0.5f) {
					finalCol = new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * outerAlpha);
				} else {
					finalCol = new Color(innerColor.r, innerColor.g, innerColor.b, innerColor.a * outerAlpha);
				}
				tex.SetPixel(x, y, finalCol);
			}
		}
		tex.Apply();
		return tex;
	}

	private float DistanceToRoundedRect(int x, int y, int width, int height, int radius) {
		int cx = x < radius ? radius : (x > width - radius - 1 ? width - radius - 1 : x);
		int cy = y < radius ? radius : (y > height - radius - 1 ? height - radius - 1 : y);
		float dx = x - cx;
		float dy = y - cy;
		return Mathf.Sqrt(dx * dx + dy * dy);
	}

	void Update() {
		if (grabController == null) return;

		bool hasTarget = grabController.TargetedGrabbable != null && !grabController.IsHoldingObject;
		bool isHolding = grabController.IsHoldingObject;

		// Smooth fade animations
		float targetPromptAlpha = hasTarget ? 1.0f : 0f;
		promptAlpha = Mathf.MoveTowards(promptAlpha, targetPromptAlpha, Time.deltaTime * 6.5f);

		float targetHoldAlpha = isHolding ? 1.0f : 0f;
		holdHudAlpha = Mathf.MoveTowards(holdHudAlpha, targetHoldAlpha, Time.deltaTime * 6.0f);

		float targetScale = hasTarget ? 1.35f : 1.0f;
		reticleScale = Mathf.Lerp(reticleScale, targetScale, Time.deltaTime * 12f);
	}

	void OnGUI() {
		if (Event.current.type != EventType.Repaint) return;
		if (grabController == null) return;

		InitStyles();

		Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

		// 1. Center Reticle / Crosshair
		if (showReticle) {
			DrawDynamicReticle(center);
		}

		// 2. '[E] Hold / Grab Item' Interaction Tooltip
		if (showPrompt && promptAlpha > 0.01f) {
			DrawGrabPrompt(center);
		}

		// 3. Held Object Controls Banner
		if (holdHudAlpha > 0.01f) {
			DrawHoldingHUD(center);
		}
	}

	private void InitStyles() {
		if (promptTextStyle == null) {
			promptTextStyle = new GUIStyle(GUI.skin.label);
			promptTextStyle.fontSize = 14;
			promptTextStyle.fontStyle = FontStyle.Bold;
			promptTextStyle.alignment = TextAnchor.MiddleCenter;
		}

		if (keyBadgeStyle == null) {
			keyBadgeStyle = new GUIStyle(GUI.skin.box);
			keyBadgeStyle.fontSize = 13;
			keyBadgeStyle.fontStyle = FontStyle.Bold;
			keyBadgeStyle.alignment = TextAnchor.MiddleCenter;
		}

		if (itemLabelStyle == null) {
			itemLabelStyle = new GUIStyle(GUI.skin.label);
			itemLabelStyle.fontSize = 15;
			itemLabelStyle.fontStyle = FontStyle.Bold;
			itemLabelStyle.alignment = TextAnchor.MiddleLeft;
		}

		if (hintTextStyle == null) {
			hintTextStyle = new GUIStyle(GUI.skin.label);
			hintTextStyle.fontSize = 13;
			hintTextStyle.fontStyle = FontStyle.Normal;
			hintTextStyle.alignment = TextAnchor.MiddleCenter;
		}
	}

	private void DrawDynamicReticle(Vector2 center) {
		bool isTargeting = (grabController.TargetedGrabbable != null && !grabController.IsHoldingObject);
		Color currentColor = Color.Lerp(reticleNormalColor, reticleTargetColor, promptAlpha);

		// Center dot
		float dotSize = 4f * reticleScale;
		GUI.color = new Color(0f, 0f, 0f, currentColor.a * 0.7f); // shadow
		GUI.DrawTexture(new Rect(center.x - dotSize * 0.5f + 1f, center.y - dotSize * 0.5f + 1f, dotSize, dotSize), whiteTex);
		GUI.color = currentColor;
		GUI.DrawTexture(new Rect(center.x - dotSize * 0.5f, center.y - dotSize * 0.5f, dotSize, dotSize), whiteTex);

		// When targeting, draw modern surrounding brackets / indicator ring
		if (promptAlpha > 0.05f) {
			float ringDist = 16f * reticleScale;
			float bracketLen = 5f;
			float bracketThick = 2f;
			GUI.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentColor.a * promptAlpha);

			// Top-Left bracket
			GUI.DrawTexture(new Rect(center.x - ringDist, center.y - ringDist, bracketLen, bracketThick), whiteTex);
			GUI.DrawTexture(new Rect(center.x - ringDist, center.y - ringDist, bracketThick, bracketLen), whiteTex);

			// Top-Right bracket
			GUI.DrawTexture(new Rect(center.x + ringDist - bracketLen, center.y - ringDist, bracketLen, bracketThick), whiteTex);
			GUI.DrawTexture(new Rect(center.x + ringDist - bracketThick, center.y - ringDist, bracketThick, bracketLen), whiteTex);

			// Bottom-Left bracket
			GUI.DrawTexture(new Rect(center.x - ringDist, center.y + ringDist - bracketThick, bracketLen, bracketThick), whiteTex);
			GUI.DrawTexture(new Rect(center.x - ringDist, center.y + ringDist - bracketLen, bracketThick, bracketLen), whiteTex);

			// Bottom-Right bracket
			GUI.DrawTexture(new Rect(center.x + ringDist - bracketLen, center.y + ringDist - bracketThick, bracketLen, bracketThick), whiteTex);
			GUI.DrawTexture(new Rect(center.x + ringDist - bracketThick, center.y + ringDist - bracketLen, bracketThick, bracketLen), whiteTex);
		}
	}

	private void DrawGrabPrompt(Vector2 center) {
		Grabbable target = grabController.TargetedGrabbable;
		string itemName = (target != null) ? target.GetDisplayName() : "Object";
		string keyString = grabController.grabKey.ToString();
		string actionText = grabController.holdToGrab ? "Hold" : "Grab";

		// Calculate box dimensions
		float keyBadgeWidth = Mathf.Max(28f, 10f + keyString.Length * 9f);
		float keyBadgeHeight = 22f;
		string labelFull = $" {actionText}  <color=#FFB444>{itemName}</color>";

		GUIContent content = new GUIContent(labelFull);
		GUIStyle richStyle = new GUIStyle(itemLabelStyle);
		richStyle.richText = true;
		richStyle.normal.textColor = Color.white;

		Vector2 textSize = richStyle.CalcSize(content);
		float totalWidth = keyBadgeWidth + textSize.x + 24f;
		float boxHeight = 36f;

		float posX = center.x - totalWidth * 0.5f;
		float posY = promptNearCrosshair ? (center.y + 28f) : (Screen.height - 110f);

		Rect boxRect = new Rect(posX, posY, totalWidth, boxHeight);

		// Draw background & border
		GUI.color = new Color(1f, 1f, 1f, promptAlpha);
		if (borderTex != null) {
			GUI.DrawTexture(boxRect, borderTex, ScaleMode.StretchToFill);
		} else {
			GUI.DrawTexture(boxRect, whiteTex);
		}

		// Draw Key Badge '[ E ]'
		Rect keyRect = new Rect(posX + 8f, posY + (boxHeight - keyBadgeHeight) * 0.5f, keyBadgeWidth, keyBadgeHeight);
		GUI.color = new Color(promptBadgeColor.r, promptBadgeColor.g, promptBadgeColor.b, promptAlpha);
		GUI.DrawTexture(keyRect, whiteTex);

		// Key text
		GUIStyle kStyle = new GUIStyle(keyBadgeStyle);
		kStyle.normal.textColor = promptBadgeTextColor;
		GUI.Label(keyRect, keyString, kStyle);

		// Label text
		Rect textRect = new Rect(posX + keyBadgeWidth + 14f, posY, textSize.x, boxHeight);
		GUI.color = new Color(1f, 1f, 1f, promptAlpha);
		GUI.Label(textRect, content, richStyle);
	}

	private void DrawHoldingHUD(Vector2 center) {
		Grabbable held = grabController.HeldGrabbable;
		string itemName = (held != null) ? held.GetDisplayName() : "Object";
		float currentDist = grabController.CurrentHoldDistance;

		float bannerWidth = 580f;
		float bannerHeight = 44f;
		float posX = center.x - bannerWidth * 0.5f;
		float posY = Screen.height - 75f;

		Rect bannerRect = new Rect(posX, posY, bannerWidth, bannerHeight);

		// Translucent background
		GUI.color = new Color(1f, 1f, 1f, holdHudAlpha * 0.9f);
		if (borderTex != null) {
			GUI.DrawTexture(bannerRect, borderTex, ScaleMode.StretchToFill);
		} else {
			GUI.DrawTexture(bannerRect, whiteTex);
		}

		// Information & Controls Guide
		GUIStyle guideStyle = new GUIStyle(hintTextStyle);
		guideStyle.richText = true;
		guideStyle.normal.textColor = Color.white;

		string dropKey = grabController.grabKey.ToString();
		string throwKey = grabController.throwKey.ToString();
		string pullKey = grabController.pullCloserKey.ToString();
		string pushKey = grabController.pushFartherKey.ToString();
		string controlsGuide = $"Holding <b><color=#FFB444>{itemName}</color></b>  •  <b>[{dropKey}]</b> Drop  •  <b>[{throwKey} / LMB]</b> Throw  •  <b>[{pullKey}/{pushKey} or Scroll]</b> Dist ({currentDist:F1}m)  •  <b>[Arrows]</b> Rotate";

		GUI.color = new Color(1f, 1f, 1f, holdHudAlpha);
		GUI.Label(bannerRect, controlsGuide, guideStyle);
	}
}
