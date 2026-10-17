using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Unity Editor utility for the "Ashes of Time" project:
/// One-click setup, balancing, and distribution of fires across the scene.
/// Adheres strictly to the theme: Time, Fire, and Shadow ("Fire everywhere, but not too much").
/// </summary>
public class FireThemeEditor : EditorWindow {
	[MenuItem("Tools/Ashes of Time/Fire & Theme Balancing Tool")]
	public static void OpenWindow() {
		var window = GetWindow<FireThemeEditor>("Fire Theme Setup");
		window.minSize = new Vector2(380, 480);
		window.Show();
	}

	[MenuItem("Tools/Ashes of Time/Quick Balance All Fire Lights")]
	public static void QuickBalanceFires() {
		BalanceSceneFireLights();
	}

	[MenuItem("Tools/Ashes of Time/Force Refresh Fire Colors (Reimport All Fire Materials)")]
	public static void ForceReimportFireAssets() {
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Shaders/SH_Vefects_VFX_URP_Particles_Erosion_01.shader", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Shaders/SH_Vefects_VFX_URP_Fire_Flames_01.shader", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Materials/M_VFX_Fire_01.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Materials/M_VFX_Fire_02.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Materials/M_VFX_Fire_Quad_01.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Materials/M_VFX_Fire_Quad_02.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Materials/M_VFX_Glow_01.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Vefects/Free Fire VFX URP/Materials/M_VFX_Ashes_01.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.ImportAsset("Assets/Laterns and candles/materials/Flame.mat", ImportAssetOptions.ForceUpdate);
		AssetDatabase.Refresh();
		BalanceSceneFireLights();
		EditorUtility.DisplayDialog("Fire Color Refreshed", "All fire shaders and materials have been re-imported in real fire orange!", "OK");
	}

	private Vector2 scrollPos;
	private float masterBrightness = 1.0f;
	private Color fireColor = new Color(1.0f, 0.42f, 0.05f, 1.0f);

	void OnGUI() {
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

		EditorGUILayout.Space(10);
		EditorGUILayout.LabelField("Ashes of Time - Fire Theme Tool", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("Theme: Time, Fire & Shadow.\nGoal: Distribute atmospheric fire across exploration areas while keeping light balanced so shadows remain deep and mysterious.", MessageType.Info);

		EditorGUILayout.Space(10);
		EditorGUILayout.LabelField("1. Balance Existing Scene Fire Lights", EditorStyles.boldLabel);
		fireColor = EditorGUILayout.ColorField("Fire Warmth Tone", fireColor);
		masterBrightness = EditorGUILayout.Slider("Master Brightness", masterBrightness, 0.5f, 2.0f);

		if (GUILayout.Button("Balance All Fire & Candle Lights", GUILayout.Height(35))) {
			BalanceSceneFireLights();
		}

		EditorGUILayout.Space(15);
		EditorGUILayout.LabelField("2. City & Map House Fires (Sides & Edges)", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("Finds every house/building across the entire map and places a subtle, reduced orange fire on its side/edge (simulating a burned city where fire has reduced).", MessageType.None);

		if (GUILayout.Button("Distribute Fires to All Houses Across Map (Sides & Edges)", GUILayout.Height(38))) {
			BakeHouseEdgeFires();
		}

		EditorGUILayout.Space(15);
		EditorGUILayout.LabelField("3. Eliminate Yellow (Enforce Pure Orange)", EditorStyles.boldLabel);
		if (GUILayout.Button("Convert All Yellow Fire Particles & Lights to Pure Orange", GUILayout.Height(32))) {
			ConvertAllYellowFiresToOrange();
		}

		EditorGUILayout.Space(15);
		EditorGUILayout.LabelField("4. Trail & World Distribution", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("Bakes atmospheric fires along the trail from the Player Spawn (Z=410) down to the Altar Sanctuary (Z=155) under '04_Random_Fires'.", MessageType.None);

		if (GUILayout.Button("Bake Trail Fires Into Scene (Direct Placement)", GUILayout.Height(32))) {
			BakeTrailFires();
		}

		EditorGUILayout.Space(15);
		EditorGUILayout.LabelField("5. Runtime Theme Manager", EditorStyles.boldLabel);
		if (GUILayout.Button("Setup FireThemeManager in Scene", GUILayout.Height(30))) {
			SetupFireThemeManager();
		}

		EditorGUILayout.EndScrollView();
	}

	[MenuItem("Tools/Ashes of Time/Distribute Orange Fires to All Houses Across Map")]
	public static void MenuDistributeHouseFires() {
		BakeHouseEdgeFires();
	}

	[MenuItem("Tools/Ashes of Time/Convert All Yellow Fires to Orange")]
	public static void MenuConvertYellowToOrange() {
		ConvertAllYellowFiresToOrange();
	}

	public static void BalanceSceneFireLights() {
		var allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
		int balancedCount = 0;
		int shadowCasters = 0;

		Undo.RecordObjects(allLights, "Balance Fire Lights");

		foreach (var l in allLights) {
			if (l == null || l.type == LightType.Directional) continue;

			string name = l.gameObject.name.ToLower();
			string parentName = l.transform.parent != null ? l.transform.parent.name.ToLower() : "";
			bool isFire = name.Contains("fire") || name.Contains("flame") ||
			              name.Contains("candle") || name.Contains("lantern") ||
			              name.Contains("latern") || parentName.Contains("fire") ||
			              parentName.Contains("candle") || parentName.Contains("lantern") ||
			              parentName.Contains("latern");

			if (!isFire) continue;

			// Set calibrated intensity
			if (name.Contains("big")) {
				l.intensity = 11.0f;
				l.range = 14.0f;
				l.shadows = LightShadows.Soft;
				shadowCasters++;
			} else if (name.Contains("small") || name.Contains("candle")) {
				l.intensity = 2.8f;
				l.range = 5.5f;
				l.shadows = LightShadows.None; // remove heavy shadow overhead on small lights
			} else {
				// Medium fire / floor fire
				l.intensity = 6.5f;
				l.range = 9.5f;
				if (shadowCasters < 6) {
					l.shadows = LightShadows.Soft;
					shadowCasters++;
				} else {
					l.shadows = LightShadows.None;
				}
			}

			// Consistent rich warm fire color
			l.color = new Color(1.0f, 0.42f, 0.05f, 1.0f);

			// Add FireFlicker if missing
			var flicker = l.GetComponent<FireFlicker>();
			if (flicker == null) {
				flicker = Undo.AddComponent<FireFlicker>(l.gameObject);
			}
			flicker.baseIntensity = l.intensity;
			flicker.warmColor = l.color;

			// Add FireSanctuary on fire root if missing
			Transform fireRoot = l.transform.parent != null ? l.transform.parent : l.transform;
			if (fireRoot.GetComponent<FireSanctuary>() == null) {
				var sanctuary = Undo.AddComponent<FireSanctuary>(fireRoot.gameObject);
				sanctuary.protectionRadius = Mathf.Max(l.range * 0.85f, 6.0f);
			}

			balancedCount++;
			EditorUtility.SetDirty(l.gameObject);
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		Debug.Log($"[FireThemeEditor] Successfully balanced {balancedCount} lights ({shadowCasters} casting dynamic soft shadows). Scene marked dirty.");
		EditorUtility.DisplayDialog("Fire Balancing Complete", $"Successfully balanced {balancedCount} fire lights.\n{shadowCasters} hero lights configured with soft shadows.\nDynamic flicker & FireSanctuary attached.", "OK");
	}

	public static void BakeTrailFires() {
		// Locate or create the container under 04_Random_Fires or active scene
		GameObject randomFiresObj = GameObject.Find("04_Random_Fires");
		Transform parentTransform = randomFiresObj != null ? randomFiresObj.transform : null;

		// Load fire prefabs
		GameObject mediumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Medium.prefab");
		GameObject smallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small.prefab");
		GameObject floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_Floor_01.prefab");
		AudioClip crackleAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Vefects/Free Fire VFX URP/Audio/SFX_FireMedium_L.wav");

		// Waypoints between player spawn (458, 16.5, 410) down to Sanctuary (452, 27, 155)
		Vector3[] positions = new Vector3[] {
			new Vector3(458.5f, 16.5f, 402.0f), // 1. Start camp
			new Vector3(465.0f, 16.2f, 375.0f), // 2. Trail fork
			new Vector3(452.0f, 16.8f, 340.0f), // 3. Flooded crossing
			new Vector3(442.0f, 17.5f, 305.0f), // 4. Waterside ruin post
			new Vector3(460.0f, 18.8f, 275.0f), // 5. Ascent ridge
			new Vector3(454.0f, 21.2f, 245.0f), // 6. Outer arch approach
			new Vector3(447.0f, 23.5f, 215.0f), // 7. Gate to Altar
			new Vector3(463.0f, 25.0f, 190.0f)  // 8. Sanctuary periphery
		};

		Terrain terrain = Terrain.activeTerrain;
		int placedCount = 0;

		for (int i = 0; i < positions.Length; i++) {
			Vector3 pos = positions[i];
			if (terrain != null) {
				float tHeight = terrain.SampleHeight(pos);
				pos.y = Mathf.Max(tHeight, pos.y);
			}

			// Alternate prefabs for variety
			GameObject prefabToUse = (i % 3 == 0) ? mediumPrefab : (i % 3 == 1 ? smallPrefab : floorPrefab);
			if (prefabToUse == null) prefabToUse = mediumPrefab;

			GameObject instance;
			if (prefabToUse != null) {
				instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse);
				instance.transform.position = pos;
				instance.transform.SetParent(parentTransform);
			} else {
				instance = new GameObject($"Trail_Fire_{i + 1:D2}");
				instance.transform.position = pos;
				instance.transform.SetParent(parentTransform);
			}

			instance.name = $"Trail_Fire_{i + 1:D2}";
			Undo.RegisterCreatedObjectUndo(instance, "Bake Trail Fire");

			// Ensure light exists and is balanced
			Light lightComp = instance.GetComponentInChildren<Light>();
			if (lightComp == null) {
				GameObject lightObj = new GameObject("FireLight");
				lightObj.transform.SetParent(instance.transform, false);
				lightObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
				lightComp = lightObj.AddComponent<Light>();
			}

			lightComp.type = LightType.Point;
			lightComp.color = new Color(1.0f, 0.42f, 0.05f, 1.0f);
			lightComp.range = (i % 3 == 0) ? 9.5f : 6.5f;
			lightComp.intensity = (i % 3 == 0) ? 6.5f : 3.5f;
			lightComp.shadows = (i % 4 == 0) ? LightShadows.Soft : LightShadows.None;

			// Add FireFlicker
			var flicker = lightComp.GetComponent<FireFlicker>();
			if (flicker == null) flicker = lightComp.gameObject.AddComponent<FireFlicker>();
			flicker.baseIntensity = lightComp.intensity;
			flicker.warmColor = lightComp.color;

			// Add FireSanctuary & 3D Audio
			var sanctuary = instance.GetComponent<FireSanctuary>();
			if (sanctuary == null) sanctuary = instance.AddComponent<FireSanctuary>();
			sanctuary.protectionRadius = lightComp.range * 0.85f;

			var audio = instance.GetComponent<AudioSource>();
			if (audio == null && crackleAudio != null) {
				audio = instance.AddComponent<AudioSource>();
				audio.clip = crackleAudio;
				audio.spatialBlend = 1.0f;
				audio.minDistance = 2.0f;
				audio.maxDistance = 15.0f;
				audio.loop = true;
				audio.volume = 0.45f;
				audio.rolloffMode = AudioRolloffMode.Linear;
				audio.playOnAwake = true;
			}

			placedCount++;
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		Debug.Log($"[FireThemeEditor] Successfully placed and balanced {placedCount} trail fires.");
		EditorUtility.DisplayDialog("Trail Fires Baked", $"Successfully placed {placedCount} fires across the exploration path.\nSaved under '04_Random_Fires'.", "OK");
	}

	public static void SetupFireThemeManager() {
		var manager = Object.FindAnyObjectByType<FireThemeManager>();
		if (manager == null) {
			GameObject themeRoot = GameObject.Find("--- THEME: FIRE, SHADOW & TIME ---");
			GameObject managerObj = new GameObject("FireThemeManager");
			if (themeRoot != null) managerObj.transform.SetParent(themeRoot.transform);

			manager = Undo.AddComponent<FireThemeManager>(managerObj);
		}

		// Assign prefab references
		manager.mediumFirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Medium.prefab");
		manager.smallFirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small.prefab");
		manager.floorFirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_Floor_01.prefab");

		manager.InitializeAndBalanceAllFires();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		EditorUtility.DisplayDialog("Manager Ready", "FireThemeManager is active and configured in the scene.", "OK");
	}

	public static void BakeHouseEdgeFires() {
		GameObject cityFiresParent = GameObject.Find("04_City_Reduced_Fires");
		if (cityFiresParent == null) {
			GameObject themeRoot = GameObject.Find("--- THEME: FIRE, SHADOW & TIME ---");
			cityFiresParent = new GameObject("04_City_Reduced_Fires");
			if (themeRoot != null) cityFiresParent.transform.SetParent(themeRoot.transform);
			Undo.RegisterCreatedObjectUndo(cityFiresParent, "Create 04_City_Reduced_Fires");
		}

		// Load prefabs
		GameObject floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_Floor_01.prefab");
		GameObject smallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small.prefab");
		GameObject mediumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Medium.prefab");
		AudioClip crackleAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Vefects/Free Fire VFX URP/Audio/SFX_FireSmall_L.wav");

		// Find all buildings across the map
		var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
		System.Collections.Generic.List<Transform> buildingRoots = new System.Collections.Generic.List<Transform>();

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

		Terrain terrain = Terrain.activeTerrain;
		int placedCount = 0;

		// Fallback curated house waypoints if scene uses prefab instances without recognizable names
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

			// Place on the side / edge of the house
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

			// Ground above water level
			if (firePos.y < 15.6f) {
				firePos.y = Mathf.Max(bounds.min.y + 0.2f, 15.7f);
			}

			// Alternate subtle fires: mostly floor and small fires for a reduced city fire aesthetic
			GameObject prefabToUse = (i % 3 == 0) ? floorPrefab : ((i % 3 == 1) ? smallPrefab : mediumPrefab);
			if (prefabToUse == null) prefabToUse = floorPrefab ?? smallPrefab;

			GameObject instance;
			if (prefabToUse != null) {
				instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse);
				instance.transform.position = firePos;
				instance.transform.SetParent(cityFiresParent.transform);
			} else {
				instance = new GameObject($"EdgeFire_{bRoot.name}_{placedCount + 1:D2}");
				instance.transform.position = firePos;
				instance.transform.SetParent(cityFiresParent.transform);
			}

			instance.name = $"EdgeFire_{bRoot.name}_{placedCount + 1:D2}";
			Undo.RegisterCreatedObjectUndo(instance, "Bake House Edge Fire");

			// Configure light (pure flame orange, smoldering reduced intensity)
			Light lightComp = instance.GetComponentInChildren<Light>();
			if (lightComp == null) {
				GameObject lightObj = new GameObject("OrangeFireLight");
				lightObj.transform.SetParent(instance.transform, false);
				lightObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
				lightComp = lightObj.AddComponent<Light>();
			}

			lightComp.type = LightType.Point;
			lightComp.color = new Color(1.0f, 0.40f, 0.04f, 1.0f); // Pure bonfire orange
			lightComp.range = (i % 3 == 0) ? 6.5f : 8.0f;
			lightComp.intensity = (i % 3 == 0) ? 3.5f : 5.0f; // Smoldering reduced intensity
			lightComp.shadows = (i % 4 == 0) ? LightShadows.Soft : LightShadows.None;

			// Add FireFlicker with deep orange range
			var flicker = lightComp.GetComponent<FireFlicker>();
			if (flicker == null) flicker = lightComp.gameObject.AddComponent<FireFlicker>();
			flicker.warmColor = new Color(1.0f, 0.30f, 0.02f, 1.0f);
			flicker.brightColor = new Color(1.0f, 0.50f, 0.06f, 1.0f);
			flicker.baseIntensity = lightComp.intensity;

			// Add FireSanctuary and crackle audio
			var sanctuary = instance.GetComponent<FireSanctuary>();
			if (sanctuary == null) sanctuary = instance.AddComponent<FireSanctuary>();
			sanctuary.protectionRadius = lightComp.range * 0.85f;

			var audio = instance.GetComponent<AudioSource>();
			if (audio == null && crackleAudio != null) {
				audio = instance.AddComponent<AudioSource>();
				audio.clip = crackleAudio;
				audio.spatialBlend = 1.0f;
				audio.minDistance = 2.0f;
				audio.maxDistance = 14.0f;
				audio.loop = true;
				audio.volume = 0.35f;
				audio.rolloffMode = AudioRolloffMode.Linear;
				audio.playOnAwake = true;
			}

			placedCount++;
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		Debug.Log($"[FireThemeEditor] Successfully placed {placedCount} edge fires on houses across the map.");
		EditorUtility.DisplayDialog("House Edge Fires Distributed", $"Successfully placed {placedCount} orange fires on the sides and edges of houses across the map.\nSaved under '04_City_Reduced_Fires'.", "OK");
	}

	public static void ConvertAllYellowFiresToOrange() {
		// 1. Convert all ParticleSystems in the scene from yellow to orange
		var particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
		int psCount = 0;

		foreach (var ps in particleSystems) {
			if (ps == null) continue;
			string name = ps.gameObject.name.ToLower();
			string pName = ps.transform.parent != null ? ps.transform.parent.name.ToLower() : "";
			bool isFire = name.Contains("fire") || name.Contains("flame") || name.Contains("glow") ||
			              name.Contains("ash") || name.Contains("smoke") || name.Contains("candle") ||
			              pName.Contains("fire") || pName.Contains("flame") || pName.Contains("candle") ||
			              pName.Contains("latern") || pName.Contains("lantern");
			if (!isFire) continue;

			Undo.RecordObject(ps, "Convert Fire Color To Orange");

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
					psCount++;
				}
			}

			var main = ps.main;
			if (main.startColor.mode == ParticleSystemGradientMode.Color) {
				Color c = main.startColor.color;
				if (c.r > 0.7f && c.g > 0.45f) {
					main.startColor = new Color(1.0f, 0.36f, 0.02f, c.a);
					psCount++;
				}
			}
		}

		// 2. Convert all Lights in the scene from yellow/white to orange
		var allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
		int lightCount = 0;

		foreach (var l in allLights) {
			if (l == null || l.type == LightType.Directional) continue;

			string name = l.gameObject.name.ToLower();
			string parentName = l.transform.parent != null ? l.transform.parent.name.ToLower() : "";
			bool isFire = name.Contains("fire") || name.Contains("flame") ||
			              name.Contains("candle") || name.Contains("lantern") ||
			              name.Contains("latern") || parentName.Contains("fire") ||
			              parentName.Contains("candle") || parentName.Contains("lantern") ||
			              parentName.Contains("latern");

			if (!isFire) continue;

			Undo.RecordObject(l, "Set Light To Pure Orange");
			l.color = new Color(1.0f, 0.40f, 0.04f, 1.0f);
			lightCount++;

			var flicker = l.GetComponent<FireFlicker>();
			if (flicker != null) {
				flicker.warmColor = new Color(1.0f, 0.30f, 0.02f, 1.0f);
				flicker.brightColor = new Color(1.0f, 0.50f, 0.06f, 1.0f);
			}
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		Debug.Log($"[FireThemeEditor] Converted {psCount} particle systems and {lightCount} lights to pure fire orange.");
		EditorUtility.DisplayDialog("Colors Converted", $"Successfully converted {psCount} particle gradients and {lightCount} lights to pure flame orange!", "OK");
	}
}

