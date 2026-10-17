using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tools and menu utilities for the Grab System.
/// Allows single-click setup of grabbable components, altar testing props, and player controller wiring.
/// </summary>
public static class GrabbableEditorTools {
	[MenuItem("Tools/Ashes of Time/Setup Grab System & Spawn Altar Props", false, 10)]
	public static void SetupGrabSystemInActiveScene() {
		// 1. Ensure Player Controller has PlayerGrabController & GrabbableHUD
		var motor = Object.FindAnyObjectByType<CharController_Motor>();
		if (motor != null) {
			Undo.RegisterCompleteObjectUndo(motor.gameObject, "Setup Grab System on Player");

			var grab = motor.GetComponent<PlayerGrabController>();
			if (grab == null) {
				grab = Undo.AddComponent<PlayerGrabController>(motor.gameObject);
			}

			var hud = motor.GetComponent<GrabbableHUD>();
			if (hud == null) {
				hud = Undo.AddComponent<GrabbableHUD>(motor.gameObject);
			}

			EditorUtility.SetDirty(motor.gameObject);
			Debug.Log("[GrabbableEditor] Configured PlayerGrabController and GrabbableHUD on " + motor.gameObject.name);
		} else {
			Debug.LogWarning("[GrabbableEditor] CharController_Motor not found in active scene. Grab system will auto-attach at runtime.");
		}

		// 2. Setup World Container & Showcase Props
		const string containerName = "--- INTERACTIVE GRABBABLE PROPS ---";
		GameObject container = GameObject.Find(containerName);
		if (container == null) {
			container = new GameObject(containerName);
			Undo.RegisterCreatedObjectUndo(container, "Create Grabbable Props Container");
		}

		var setup = container.GetComponent<GrabbableWorldSetup>();
		if (setup == null) {
			setup = Undo.AddComponent<GrabbableWorldSetup>(container);
		}

		setup.SetupAltarDemonstrationProps();
		setup.EnableAllWorldPropsAsGrabbable();

		EditorUtility.SetDirty(container);
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		Debug.Log("[GrabbableEditor] Grab System and showcase altar props successfully set up!");
	}

	[MenuItem("Tools/Ashes of Time/Make Selected Object Grabbable", false, 20)]
	public static void MakeSelectedGrabbable() {
		GameObject go = Selection.activeGameObject;
		if (go == null) {
			EditorUtility.DisplayDialog("Make Grabbable", "Please select a GameObject in the hierarchy first.", "OK");
			return;
		}

		Undo.RegisterCompleteObjectUndo(go, "Make Object Grabbable");

		// Collider
		var col = go.GetComponentInChildren<Collider>();
		if (col == null) {
			var box = Undo.AddComponent<BoxCollider>(go);
			var mf = go.GetComponentInChildren<MeshFilter>();
			if (mf != null && mf.sharedMesh != null) {
				box.size = mf.sharedMesh.bounds.size;
				box.center = mf.sharedMesh.bounds.center;
			}
		}

		// Rigidbody
		var rb = go.GetComponent<Rigidbody>();
		if (rb == null) {
			rb = Undo.AddComponent<Rigidbody>(go);
			rb.mass = 1.8f;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
		}

		// Grabbable
		var grabbable = go.GetComponent<Grabbable>();
		if (grabbable == null) {
			grabbable = Undo.AddComponent<Grabbable>(go);
		}

		EditorUtility.SetDirty(go);
		Debug.Log($"[GrabbableEditor] Successfully made '{go.name}' grabbable!");
	}

	[MenuItem("Tools/Ashes of Time/Make Selected Object Grabbable", true)]
	public static bool ValidateMakeSelectedGrabbable() {
		return Selection.activeGameObject != null;
	}
}
