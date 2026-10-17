using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// World initializer for the Grab System.
/// Automatically prepares interactive grabbable props near the player spawn at Altar Ruins,
/// and dynamically equips lanterns, candles, and vases across the map with physics & Grabbable components.
/// </summary>
public class GrabbableWorldSetup : MonoBehaviour {
	private const string CONTAINER_NAME = "--- INTERACTIVE GRABBABLE PROPS ---";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void InitializeWorldGrabbables() {
		GameObject container = GameObject.Find(CONTAINER_NAME);
		if (container == null) {
			container = new GameObject(CONTAINER_NAME);
		}

		if (container.GetComponent<GrabbableWorldSetup>() == null) {
			container.AddComponent<GrabbableWorldSetup>();
		}
	}

	void Awake() {
		SetupAltarDemonstrationProps();
		EnableAllWorldPropsAsGrabbable();
	}

	/// <summary>
	/// Spawns showcase interactive props right in front of the player's spawn at Altar Ruins (456, 29, 154).
	/// </summary>
	public void SetupAltarDemonstrationProps() {
		Transform container = transform;

		// Skip if already populated
		if (container.childCount >= 3) return;

		// Player spawn is at approx (456.03, 28.99, 153.83).
		// We arrange showcase items on the stone dais right in front of the player (facing north-east Z ~ 156-157).
		Vector3 centerPos = new Vector3(456.0f, 29.15f, 156.4f);

#if UNITY_EDITOR
		GameObject lanternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Laterns and candles/Prefabs/Latern1.prefab");
		GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SineVFX/TranslucentCrystals/Prefabs/Crystalsv01.prefab");
		GameObject vasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Props/Prop_Vase_A.prefab");
		GameObject brickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Altar_Ruins_FREE/Prefabs/Brick.prefab");

		// 1. Showcase Carryable Lantern (Lights up shadows & repels stalker!)
		if (lanternPrefab != null) {
			Vector3 pos = centerPos + new Vector3(0.0f, 0.15f, 0.0f);
			GameObject lantern = Instantiate(lanternPrefab, pos, Quaternion.identity, container);
			lantern.name = "Showcase_Ancient_Lantern";
			SetupLanternGrabbable(lantern);
		}

		// 2. Showcase Mystical Altar Crystal Relic
		if (crystalPrefab != null) {
			Vector3 pos = centerPos + new Vector3(1.2f, 0.15f, -0.3f);
			GameObject crystal = Instantiate(crystalPrefab, pos, Quaternion.identity, container);
			crystal.name = "Showcase_Altar_Crystal";
			SetupCrystalGrabbable(crystal);
		}

		// 3. Showcase Ancient Ceramic Vase
		if (vasePrefab != null) {
			Vector3 pos = centerPos + new Vector3(-1.2f, 0.15f, -0.3f);
			GameObject vase = Instantiate(vasePrefab, pos, Quaternion.identity, container);
			vase.name = "Showcase_Ceramic_Vase";
			SetupVaseGrabbable(vase);
		}

		// 4. Showcase Stackable Ruin Bricks
		if (brickPrefab != null) {
			Vector3 pos1 = centerPos + new Vector3(0.6f, 0.05f, 0.7f);
			Vector3 pos2 = centerPos + new Vector3(0.65f, 0.35f, 0.68f);
			GameObject brick1 = Instantiate(brickPrefab, pos1, Quaternion.identity, container);
			brick1.name = "Showcase_Ruin_Brick_01";
			SetupBrickGrabbable(brick1);

			GameObject brick2 = Instantiate(brickPrefab, pos2, Quaternion.Euler(0f, 25f, 0f), container);
			brick2.name = "Showcase_Ruin_Brick_02";
			SetupBrickGrabbable(brick2);
		}
#endif
		Debug.Log("[GrabbableWorldSetup] Spawned showcase grabbable items in front of player at Altar Ruins.");
	}

	public static void SetupLanternGrabbable(GameObject lantern) {
		if (lantern == null) return;

		// Ensure collider
		var col = lantern.GetComponent<Collider>();
		if (col == null) {
			var cap = lantern.AddComponent<CapsuleCollider>();
			cap.radius = 0.22f;
			cap.height = 0.72f;
			cap.center = new Vector3(0f, 0.32f, 0f);
		}

		// Ensure Rigidbody
		var rb = lantern.GetComponent<Rigidbody>();
		if (rb == null) rb = lantern.AddComponent<Rigidbody>();
		rb.mass = 1.4f;
		rb.interpolation = RigidbodyInterpolation.Interpolate;

		// Ensure Grabbable
		var grabbable = lantern.GetComponent<Grabbable>();
		if (grabbable == null) grabbable = lantern.AddComponent<Grabbable>();
		grabbable.objectDisplayName = "Ancient Lantern";
		grabbable.holdDistance = 1.75f;
		grabbable.holdRotationOffset = new Vector3(0f, 0f, 0f);

		// Ensure Light & Sanctuary
		var light = lantern.GetComponentInChildren<Light>();
		if (light == null) {
			GameObject lObj = new GameObject("LanternLight");
			lObj.transform.SetParent(lantern.transform, false);
			lObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
			light = lObj.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = new Color(1.0f, 0.55f, 0.12f);
			light.range = 8.5f;
			light.intensity = 5.0f;
			light.shadows = LightShadows.Soft;

			var flicker = lObj.AddComponent<FireFlicker>();
			flicker.baseIntensity = 5.0f;
			flicker.warmColor = light.color;
		}

		if (lantern.GetComponent<FireSanctuary>() == null) {
			var sanctuary = lantern.AddComponent<FireSanctuary>();
			sanctuary.protectionRadius = 7.0f;
		}
	}

	public static void SetupCrystalGrabbable(GameObject crystal) {
		if (crystal == null) return;

		var col = crystal.GetComponent<Collider>();
		if (col == null) {
			var box = crystal.AddComponent<BoxCollider>();
			box.size = new Vector3(0.45f, 0.75f, 0.45f);
			box.center = new Vector3(0f, 0.35f, 0f);
		}

		var rb = crystal.GetComponent<Rigidbody>();
		if (rb == null) rb = crystal.AddComponent<Rigidbody>();
		rb.mass = 2.2f;
		rb.interpolation = RigidbodyInterpolation.Interpolate;

		var grabbable = crystal.GetComponent<Grabbable>();
		if (grabbable == null) grabbable = crystal.AddComponent<Grabbable>();
		grabbable.objectDisplayName = "Altar Relic Crystal";
		grabbable.holdDistance = 1.65f;

		// Ambient crystal glow
		var light = crystal.GetComponentInChildren<Light>();
		if (light == null) {
			GameObject lObj = new GameObject("CrystalGlow");
			lObj.transform.SetParent(crystal.transform, false);
			lObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
			light = lObj.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = new Color(0.25f, 0.85f, 1.0f);
			light.range = 4.5f;
			light.intensity = 2.8f;
		}
	}

	public static void SetupVaseGrabbable(GameObject vase) {
		if (vase == null) return;

		var rb = vase.GetComponent<Rigidbody>();
		if (rb == null) rb = vase.AddComponent<Rigidbody>();
		rb.mass = 1.6f;
		rb.interpolation = RigidbodyInterpolation.Interpolate;

		// If root doesn't have a collider, add one matching child bounds
		var col = vase.GetComponent<Collider>();
		if (col == null && vase.GetComponentInChildren<Collider>() == null) {
			var box = vase.AddComponent<BoxCollider>();
			box.size = new Vector3(0.4f, 0.65f, 0.4f);
			box.center = new Vector3(0f, 0.3f, 0f);
		}

		var grabbable = vase.GetComponent<Grabbable>();
		if (grabbable == null) grabbable = vase.AddComponent<Grabbable>();
		grabbable.objectDisplayName = "Ceramic Vase";
		grabbable.holdDistance = 1.8f;
	}

	public static void SetupBrickGrabbable(GameObject brick) {
		if (brick == null) return;

		var col = brick.GetComponent<Collider>();
		if (col == null) {
			var box = brick.AddComponent<BoxCollider>();
			box.size = new Vector3(0.55f, 0.28f, 0.32f);
			box.center = new Vector3(0f, 0.14f, 0f);
		}

		var rb = brick.GetComponent<Rigidbody>();
		if (rb == null) rb = brick.AddComponent<Rigidbody>();
		rb.mass = 3.5f;
		rb.interpolation = RigidbodyInterpolation.Interpolate;

		var grabbable = brick.GetComponent<Grabbable>();
		if (grabbable == null) grabbable = brick.AddComponent<Grabbable>();
		grabbable.objectDisplayName = "Ruin Stone Brick";
		grabbable.holdDistance = 1.55f;
	}

	/// <summary>
	/// Scans existing lanterns, candles, and vases across the scene and ensures they are physics-grabbable.
	/// </summary>
	public void EnableAllWorldPropsAsGrabbable() {
		int enabledCount = 0;
		var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);

		foreach (var go in allObjects) {
			if (go == null) continue;
			if (go.GetComponent<Grabbable>() != null) continue;

			string name = go.name.ToLower();
			bool isProp = (name.Contains("latern") || name.Contains("lantern") ||
			               name.Contains("candle") || name.Contains("vase") ||
			               name.Contains("chalice") || name.Contains("skull"));

			if (!isProp) continue;

			// Don't convert static large structures or lamp posts
			if (name.Contains("post") || name.Contains("wall") || name.Contains("ceiling") || name.Contains("terrain")) {
				continue;
			}

			// Add collider if missing
			var col = go.GetComponentInChildren<Collider>();
			if (col == null) {
				var box = go.AddComponent<BoxCollider>();
				box.size = Vector3.one * 0.4f;
				box.center = new Vector3(0f, 0.2f, 0f);
			}

			// Add Rigidbody
			var rb = go.GetComponent<Rigidbody>();
			if (rb == null) {
				rb = go.AddComponent<Rigidbody>();
				rb.mass = 1.5f;
				rb.interpolation = RigidbodyInterpolation.Interpolate;
			}

			// Add Grabbable
			var grabbable = go.AddComponent<Grabbable>();
			if (name.Contains("latern") || name.Contains("lantern")) {
				grabbable.objectDisplayName = "Lantern";
				var sanctuary = go.GetComponent<FireSanctuary>();
				if (sanctuary == null) {
					sanctuary = go.AddComponent<FireSanctuary>();
					sanctuary.protectionRadius = 6.0f;
				}
			} else if (name.Contains("candle")) {
				grabbable.objectDisplayName = "Candle";
			} else if (name.Contains("vase")) {
				grabbable.objectDisplayName = "Ceramic Vase";
			}

			enabledCount++;
		}

		if (enabledCount > 0) {
			Debug.Log($"[GrabbableWorldSetup] Initialized {enabledCount} world props as grabbable.");
		}
	}
}
