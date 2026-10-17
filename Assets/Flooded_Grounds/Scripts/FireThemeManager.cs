using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oversees the Fire theme across the game.
/// Ensures fires are distributed throughout the world ("fire everywhere, but not too much"),
/// balances light intensities so they do not overpower the Shadow theme,
/// and adds natural flickering and audio to all fire sources.
/// </summary>
public class FireThemeManager : MonoBehaviour {
	public static FireThemeManager Instance { get; private set; }

	[Header("Master Fire Tuning")]
	[Range(0.2f, 2.5f)]
	public float globalFireBrightness = 1.0f;
	[Tooltip("Rich, authentic bonfire orange color used for all fire lighting.")]
	public Color masterFireColor = new Color(1.0f, 0.42f, 0.05f, 1.0f);
	[Tooltip("If true, all fire lights will dynamically flicker.")]
	public bool enableOrganicFlicker = true;
	[Tooltip("Limit realtime shadow casting to hero fires to maintain deep shadows and high FPS in URP.")]
	public bool optimizeShadows = true;
	[Range(1, 10)]
	public int maxShadowCasters = 6;

	[Header("Atmospheric Trail & City Fires")]
	[Tooltip("Automatically spawns subtle trail fires along the exploration route if not already baked.")]
	public bool spawnTrailFiresIfMissing = true;
	[Tooltip("Automatically spawns reduced orange fires on the sides and edges of every house across the map.")]
	public bool spawnHouseEdgeFiresIfMissing = true;
	public GameObject smallFirePrefab;
	public GameObject mediumFirePrefab;
	public GameObject floorFirePrefab;

	[Header("Runtime Cache")]
	[SerializeField] private List<Light> managedFireLights = new List<Light>();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void AutoEnsureManager() {
		if (Instance == null && Object.FindAnyObjectByType<FireThemeManager>() == null) {
			GameObject root = GameObject.Find("--- THEME: FIRE, SHADOW & TIME ---");
			GameObject go = new GameObject("FireThemeManager");
			if (root != null) go.transform.SetParent(root.transform);
			go.AddComponent<FireThemeManager>();
		}
	}

	void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
			return;
		}
		Instance = this;

#if UNITY_EDITOR
		if (mediumFirePrefab == null)
			mediumFirePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Medium.prefab");
		if (smallFirePrefab == null)
			smallFirePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small.prefab");
		if (floorFirePrefab == null)
			floorFirePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_Floor_01.prefab");
#endif

		InitializeAndBalanceAllFires();
		ConvertParticleYellowToOrange();

		if (spawnTrailFiresIfMissing) {
			SpawnExplorationTrailFires();
		}
		if (spawnHouseEdgeFiresIfMissing) {
			SpawnHouseEdgeFiresRuntime();
		}
	}

	/// <summary>
	/// Finds all fire, lantern, and candle lights and balances them:
	/// clamps crazy intensities (25-50 down to 4-10), adds flicker, sets warm color.
	/// </summary>
	[ContextMenu("Balance All Fires Now")]
	public void InitializeAndBalanceAllFires() {
		managedFireLights.Clear();
		var allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
		int shadowCastersCount = 0;

		foreach (var l in allLights) {
			if (l == null || l.type == LightType.Directional) continue;

			string objName = l.gameObject.name.ToLower();
			string parentName = l.transform.parent != null ? l.transform.parent.name.ToLower() : "";
			bool isFireRelated = objName.Contains("fire") || objName.Contains("flame") ||
			                     objName.Contains("candle") || objName.Contains("lantern") ||
			                     objName.Contains("latern") || parentName.Contains("fire") ||
			                     parentName.Contains("candle") || parentName.Contains("lantern") ||
			                     parentName.Contains("latern");

			if (!isFireRelated) continue;

			managedFireLights.Add(l);

			// 1. Balance Light Intensities ("Not too much")
			if (objName.Contains("big")) {
				l.intensity = Mathf.Clamp(l.intensity, 8f, 14f) * globalFireBrightness;
				l.range = Mathf.Clamp(l.range, 12f, 16f);
			} else if (objName.Contains("small") || objName.Contains("candle")) {
				l.intensity = Mathf.Clamp(l.intensity, 1.8f, 3.5f) * globalFireBrightness;
				l.range = Mathf.Clamp(l.range, 4f, 7f);
			} else {
				// Medium fire / default
				l.intensity = Mathf.Clamp(l.intensity, 4.5f, 8.5f) * globalFireBrightness;
				l.range = Mathf.Clamp(l.range, 8f, 12f);
			}

			// 2. Consistent warm fire tone
			l.color = masterFireColor;

			// 3. Shadow optimization (save URP passes, keep shadows mysterious)
			if (optimizeShadows) {
				bool isHero = objName.Contains("big") || (objName.Contains("medium") && shadowCastersCount < maxShadowCasters);
				if (isHero) {
					l.shadows = LightShadows.Soft;
					shadowCastersCount++;
				} else {
					l.shadows = LightShadows.None;
				}
			}

			// 4. Attach Organic Fire Flicker
			if (enableOrganicFlicker) {
				var flicker = l.GetComponent<FireFlicker>();
				if (flicker == null) {
					flicker = l.gameObject.AddComponent<FireFlicker>();
				}
				flicker.baseIntensity = l.intensity;
				flicker.warmColor = masterFireColor;
			}

			// 5. Attach FireSanctuary on parent / fire root
			Transform fireRoot = l.transform.parent != null ? l.transform.parent : l.transform;
			if (fireRoot.GetComponent<FireSanctuary>() == null) {
				var sanctuary = fireRoot.gameObject.AddComponent<FireSanctuary>();
				sanctuary.protectionRadius = Mathf.Max(l.range * 0.8f, 6.0f);
			}
		}

		Debug.Log($"[FireThemeManager] Balanced {managedFireLights.Count} fire/candle lights ({shadowCastersCount} casting dynamic shadows).");
	}

	/// <summary>
	/// Generates curated, atmospheric fires along the journey between Player Spawn and Altar Ruins.
	/// </summary>
	private void SpawnExplorationTrailFires() {
		// If trail fires container already exists, skip duplicate spawning
		Transform existingTrail = transform.Find("04_Exploration_Trail_Fires");
		if (existingTrail != null && existingTrail.childCount > 0) return;

		// Create container under this Theme manager
		GameObject trailContainer = new GameObject("04_Exploration_Trail_Fires");
		trailContainer.transform.SetParent(transform, false);

		// Key waypoints connecting Spawn (458, 16.5, 410) -> Mid Crossing -> Altar (452, 27, 155)
		Vector3[] trailWaypoints = new Vector3[] {
			new Vector3(458.5f, 16.5f, 402.0f), // 1. Player Camp / Start Sanctuary
			new Vector3(465.0f, 16.2f, 375.0f), // 2. Trail fork entrance
			new Vector3(452.0f, 16.8f, 340.0f), // 3. Flooded path landmark
			new Vector3(442.0f, 17.5f, 305.0f), // 4. Waterside ruin post
			new Vector3(460.0f, 18.8f, 275.0f), // 5. Ascent ridge
			new Vector3(454.0f, 21.2f, 245.0f), // 6. Outer arch approach
			new Vector3(447.0f, 23.5f, 215.0f), // 7. Gate to Altar
			new Vector3(463.0f, 25.0f, 190.0f)  // 8. Sanctuary periphery
		};

		Terrain terrain = Terrain.activeTerrain;

		for (int i = 0; i < trailWaypoints.Length; i++) {
			Vector3 pos = trailWaypoints[i];
			if (terrain != null) {
				float tHeight = terrain.SampleHeight(pos);
				pos.y = Mathf.Max(tHeight, pos.y);
			}

			// Choose prefab if available, or create dynamic balanced fire
			GameObject fireObj = null;
			if (i % 2 == 0 && mediumFirePrefab != null) {
				fireObj = Instantiate(mediumFirePrefab, pos, Quaternion.identity, trailContainer.transform);
			} else if (smallFirePrefab != null) {
				fireObj = Instantiate(smallFirePrefab, pos, Quaternion.identity, trailContainer.transform);
			} else if (floorFirePrefab != null) {
				fireObj = Instantiate(floorFirePrefab, pos, Quaternion.identity, trailContainer.transform);
			} else {
				// Standalone procedural fire anchor with light, flicker, and sanctuary
				fireObj = CreateProceduralFireAnchor(pos, i, trailContainer.transform);
			}

			if (fireObj != null) {
				fireObj.name = $"Trail_Fire_{i + 1:D2}";
				var sanctuary = fireObj.GetComponent<FireSanctuary>();
				if (sanctuary == null) sanctuary = fireObj.AddComponent<FireSanctuary>();
				sanctuary.protectionRadius = 7.5f;
			}
		}

		Debug.Log($"[FireThemeManager] Placed {trailWaypoints.Length} atmospheric trail fires along the exploration route.");
	}

	private GameObject CreateProceduralFireAnchor(Vector3 pos, int index, Transform parent) {
		GameObject anchor = new GameObject($"Trail_Fire_{index + 1:D2}");
		anchor.transform.position = pos;
		anchor.transform.SetParent(parent);

		GameObject lightObj = new GameObject("FireLight");
		lightObj.transform.SetParent(anchor.transform, false);
		lightObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);

		Light l = lightObj.AddComponent<Light>();
		l.type = LightType.Point;
		l.color = masterFireColor;
		l.range = 8.5f;
		l.intensity = 5.5f * globalFireBrightness;
		l.shadows = (index % 3 == 0) ? LightShadows.Soft : LightShadows.None;

		var flicker = lightObj.AddComponent<FireFlicker>();
		flicker.baseIntensity = l.intensity;
		flicker.warmColor = masterFireColor;

		managedFireLights.Add(l);
		return anchor;
	}

	public void SetGlobalBrightness(float multiplier) {
		globalFireBrightness = multiplier;
		foreach (var l in managedFireLights) {
			if (l == null) continue;
			var flicker = l.GetComponent<FireFlicker>();
			if (flicker != null) {
				flicker.SetBaseIntensity(flicker.baseIntensity * multiplier);
			} else {
				l.intensity *= multiplier;
			}
		}
	}

	/// <summary>
	/// Converts any yellow gradient color keys in particle systems to pure real flame orange.
	/// </summary>
	public void ConvertParticleYellowToOrange() {
		var particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
		foreach (var ps in particleSystems) {
			if (ps == null) continue;
			string name = ps.gameObject.name.ToLower();
			string pName = ps.transform.parent != null ? ps.transform.parent.name.ToLower() : "";
			bool isFire = name.Contains("fire") || name.Contains("flame") || name.Contains("glow") ||
			              name.Contains("ash") || name.Contains("smoke") || name.Contains("candle") ||
			              pName.Contains("fire") || pName.Contains("flame") || pName.Contains("candle") ||
			              pName.Contains("latern") || pName.Contains("lantern");
			if (!isFire) continue;

			var col = ps.colorOverLifetime;
			if (col.enabled && col.color.mode == ParticleSystemGradientMode.Gradient) {
				Gradient grad = col.color.gradient;
				GradientColorKey[] colorKeys = grad.colorKeys;
				bool modified = false;
				for (int i = 0; i < colorKeys.Length; i++) {
					if (colorKeys[i].color.r > 0.7f && colorKeys[i].color.g > 0.45f) {
						colorKeys[i].color = new Color(1.0f, Mathf.Min(colorKeys[i].color.g, 0.36f), 0.02f);
						modified = true;
					}
				}
				if (modified) {
					grad.colorKeys = colorKeys;
					col.color = grad;
				}
			}

			var main = ps.main;
			if (main.startColor.mode == ParticleSystemGradientMode.Color) {
				Color c = main.startColor.color;
				if (c.r > 0.7f && c.g > 0.45f) {
					main.startColor = new Color(1.0f, 0.36f, 0.02f, c.a);
				}
			}
		}
	}

	/// <summary>
	/// Dynamically spawns subtle, smoldering orange fires on the sides and edges of all houses across the map.
	/// </summary>
	private void SpawnHouseEdgeFiresRuntime() {
		GameObject cityFiresParent = GameObject.Find("04_City_Reduced_Fires");
		if (cityFiresParent != null && cityFiresParent.transform.childCount > 5) return;

		if (cityFiresParent == null) {
			GameObject themeRoot = GameObject.Find("--- THEME: FIRE, SHADOW & TIME ---");
			cityFiresParent = new GameObject("04_City_Reduced_Fires");
			if (themeRoot != null) cityFiresParent.transform.SetParent(themeRoot.transform, false);
			else cityFiresParent.transform.SetParent(transform, false);
		}

		var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
		List<Transform> buildingRoots = new List<Transform>();

		foreach (var r in renderers) {
			if (r == null) continue;
			Transform cur = r.transform;
			Transform identifiedBuilding = null;

			while (cur != null && cur.parent != null) {
				string n = cur.name.ToLower();
				if (n.Contains("house") || n.Contains("cabin") || n.Contains("barn") ||
				    n.Contains("building") || n.Contains("villa") || n.Contains("guardhouse") ||
				    n.Contains("lighthouse") || n.Contains("greenhouse") || n.Contains("structures") ||
				    n.Contains("church") || n.Contains("floodedbuilding") || n.Contains("ruin")) {
					identifiedBuilding = cur;
				}
				cur = cur.parent;
			}

			if (identifiedBuilding != null) {
				bool tooClose = false;
				for (int b = 0; b < buildingRoots.Count; b++) {
					if (Vector3.Distance(buildingRoots[b].position, identifiedBuilding.position) < 14f) {
						tooClose = true;
						break;
					}
				}
				if (!tooClose) {
					buildingRoots.Add(identifiedBuilding);
				}
			}
		}

		// Curated fallback house waypoints across the flooded city map
		if (buildingRoots.Count < 4) {
			Vector3[] cityHouseCoords = new Vector3[] {
				new Vector3(485f, 17f, 395f), new Vector3(430f, 17f, 415f),
				new Vector3(510f, 18f, 360f), new Vector3(420f, 18f, 350f),
				new Vector3(475f, 19f, 310f), new Vector3(435f, 20f, 280f),
				new Vector3(490f, 22f, 250f), new Vector3(425f, 23f, 225f),
				new Vector3(480f, 25f, 195f), new Vector3(430f, 26f, 165f),
				new Vector3(835f, 17f, 440f), new Vector3(850f, 17f, 420f)
			};
			for (int i = 0; i < cityHouseCoords.Length; i++) {
				GameObject placeholder = new GameObject($"House_Cluster_{i + 1:D2}");
				placeholder.transform.position = cityHouseCoords[i];
				buildingRoots.Add(placeholder.transform);
			}
		}

		Terrain terrain = Terrain.activeTerrain;
		int placedCount = 0;

		for (int i = 0; i < buildingRoots.Count; i++) {
			Transform bRoot = buildingRoots[i];
			Bounds bounds = new Bounds(bRoot.position, Vector3.one * 4f);
			var bRenderers = bRoot.GetComponentsInChildren<Renderer>();
			if (bRenderers.Length > 0) {
				bounds = bRenderers[0].bounds;
				for (int r = 1; r < bRenderers.Length; r++) {
					bounds.Encapsulate(bRenderers[r].bounds);
				}
			}

			Vector3 edgeOffset;
			int sideChoice = i % 4;
			if (sideChoice == 0) edgeOffset = Vector3.right * (bounds.extents.x + 0.8f);
			else if (sideChoice == 1) edgeOffset = -Vector3.right * (bounds.extents.x + 0.8f);
			else if (sideChoice == 2) edgeOffset = Vector3.forward * (bounds.extents.z + 0.8f);
			else edgeOffset = -Vector3.forward * (bounds.extents.z + 0.8f);

			Vector3 firePos = new Vector3(bounds.center.x + edgeOffset.x, bounds.min.y, bounds.center.z + edgeOffset.z);

			if (terrain != null) {
				float tHeight = terrain.SampleHeight(firePos);
				firePos.y = Mathf.Max(tHeight, firePos.y);
			}

			if (firePos.y < 15.6f) {
				firePos.y = Mathf.Max(bounds.min.y + 0.2f, 15.7f);
			}

			GameObject prefabToUse = (i % 3 == 0) ? floorFirePrefab : ((i % 3 == 1) ? smallFirePrefab : mediumFirePrefab);
			GameObject fireInstance = null;
			if (prefabToUse != null) {
				fireInstance = Instantiate(prefabToUse, firePos, Quaternion.identity, cityFiresParent.transform);
			} else {
				fireInstance = CreateProceduralFireAnchor(firePos, i, cityFiresParent.transform);
			}

			if (fireInstance != null) {
				fireInstance.name = $"EdgeFire_{bRoot.name}_{placedCount + 1:D2}";
				Light l = fireInstance.GetComponentInChildren<Light>();
				if (l == null) {
					GameObject lObj = new GameObject("OrangeFireLight");
					lObj.transform.SetParent(fireInstance.transform, false);
					lObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
					l = lObj.AddComponent<Light>();
				}
				l.type = LightType.Point;
				l.color = masterFireColor;
				l.range = (i % 3 == 0) ? 6.5f : 8.0f;
				l.intensity = (i % 3 == 0) ? 3.5f : 5.0f;
				l.shadows = (i % 4 == 0) ? LightShadows.Soft : LightShadows.None;

				var flicker = l.GetComponent<FireFlicker>();
				if (flicker == null) flicker = l.gameObject.AddComponent<FireFlicker>();
				flicker.warmColor = new Color(1.0f, 0.30f, 0.02f, 1.0f);
				flicker.brightColor = new Color(1.0f, 0.50f, 0.06f, 1.0f);
				flicker.baseIntensity = l.intensity;

				var sanctuary = fireInstance.GetComponent<FireSanctuary>();
				if (sanctuary == null) sanctuary = fireInstance.AddComponent<FireSanctuary>();
				sanctuary.protectionRadius = l.range * 0.85f;

				managedFireLights.Add(l);
				placedCount++;
			}
		}

		Debug.Log($"[FireThemeManager] Distributed {placedCount} edge fires on houses across the map.");
	}
}

