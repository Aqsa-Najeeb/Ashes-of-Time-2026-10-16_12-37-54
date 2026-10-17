using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// First-person physics grab controller.
/// Detects grabbable objects in front of the camera, displays interactive prompts,
/// allows holding, distance scrolling, smooth physics manipulation, throwing, and object rotation.
/// </summary>
public class PlayerGrabController : MonoBehaviour {
	[Header("Targeting & Reach")]
	[Tooltip("Maximum reach distance in meters to grab an object.")]
	public float reachDistance = 3.6f;
	[Tooltip("Radius for spherecast check to make aiming comfortable and forgiving.")]
	public float sphereCastRadius = 0.15f;
	[Tooltip("Layers to test for grabbable objects (defaults to default and prop layers).")]
	public LayerMask interactionLayers = ~0;

	[Header("Holding Physics")]
	public float minHoldDistance = 1.1f;
	public float maxHoldDistance = 3.6f;
	public float followSpeed = 18.0f;
	public float rotationSpeed = 14.0f;
	public float maxHoldVelocity = 22.0f;
	[Tooltip("If the object is separated farther than this distance (e.g. caught behind a pillar), drop it.")]
	public float breakDistance = 4.2f;

	[Header("Throwing")]
	public float throwForce = 14.0f;
	public float throwUpwardBias = 1.2f;

	[Header("Keyboard Controls (Customizable)")]
	[Tooltip("Key to grab or drop objects.")]
	public KeyCode grabKey = KeyCode.E;
	[Tooltip("Keyboard key to throw held objects.")]
	public KeyCode throwKey = KeyCode.F;
	[Tooltip("Keyboard key to push held object farther away.")]
	public KeyCode pushFartherKey = KeyCode.R;
	[Tooltip("Keyboard key to pull held object closer.")]
	public KeyCode pullCloserKey = KeyCode.C;
	[Tooltip("Keyboard keys to rotate held object in 3D.")]
	public KeyCode rotatePitchUpKey = KeyCode.UpArrow;
	public KeyCode rotatePitchDownKey = KeyCode.DownArrow;
	public KeyCode rotateYawLeftKey = KeyCode.LeftArrow;
	public KeyCode rotateYawRightKey = KeyCode.RightArrow;
	[Tooltip("Speed when adjusting distance using keyboard keys (m/s).")]
	public float keyDistanceSpeed = 2.4f;
	[Tooltip("Speed when rotating with keyboard keys (deg/s).")]
	public float keyRotateSpeed = 85.0f;
	[Tooltip("If true, player must hold the key to carry and releases to drop. If false, toggle to grab/drop.")]
	public bool holdToGrab = false;

	[Header("Mouse Controls (Optional Alternatives)")]
	public bool allowMouseThrow = true;
	public bool allowMouseScrollDistance = true;
	public float scrollSensitivity = 0.5f;
	public bool allowMouseRotate = true;
	public float rotateSensitivity = 3.0f;

	[Header("References (Auto-detected if unassigned)")]
	public Camera playerCamera;
	public CharacterController playerCharacter;
	public Transform holdPoint;

	// Public runtime state for HUD
	public Grabbable TargetedGrabbable { get; private set; }
	public Grabbable HeldGrabbable { get; private set; }
	public bool IsHoldingObject => HeldGrabbable != null;
	public float CurrentHoldDistance { get; private set; } = 2.0f;
	public bool IsRotatingHeldObject { get; private set; } = false;

	// Internal state
	private Quaternion userRotationOffset = Quaternion.identity;
	private Collider[] heldColliders;
	private CharController_Motor playerMotor;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void AutoEnsureGrabController() {
		var motor = Object.FindAnyObjectByType<CharController_Motor>();
		if (motor != null) {
			if (motor.GetComponent<PlayerGrabController>() == null) {
				var grab = motor.gameObject.AddComponent<PlayerGrabController>();
				if (motor.GetComponent<GrabbableHUD>() == null) {
					motor.gameObject.AddComponent<GrabbableHUD>();
				}
				Debug.Log("[PlayerGrabController] Auto-attached Grab System and HUD to Player Controller.");
			}
		}
	}

	void Awake() {
		AutoDetectReferences();
		EnsureHoldPoint();
	}

	void Start() {
		AutoDetectReferences();
		EnsureHoldPoint();
		if (GetComponent<GrabbableHUD>() == null) {
			gameObject.AddComponent<GrabbableHUD>();
		}
	}

	private void AutoDetectReferences() {
		if (playerMotor == null) {
			playerMotor = GetComponent<CharController_Motor>();
			if (playerMotor == null) playerMotor = GetComponentInParent<CharController_Motor>();
		}

		if (playerCamera == null) {
			if (playerMotor != null && playerMotor.cam != null) {
				playerCamera = playerMotor.cam.GetComponent<Camera>();
			}
			if (playerCamera == null) {
				playerCamera = GetComponentInChildren<Camera>();
			}
			if (playerCamera == null) {
				playerCamera = Camera.main;
			}
		}

		if (playerCharacter == null) {
			playerCharacter = GetComponent<CharacterController>();
			if (playerCharacter == null) playerCharacter = GetComponentInParent<CharacterController>();
		}
	}

	private void EnsureHoldPoint() {
		if (holdPoint == null && playerCamera != null) {
			Transform existing = playerCamera.transform.Find("HoldAnchor");
			if (existing != null) {
				holdPoint = existing;
			} else {
				GameObject hp = new GameObject("HoldAnchor");
				hp.transform.SetParent(playerCamera.transform, false);
				hp.transform.localPosition = new Vector3(0f, -0.1f, 2.0f);
				holdPoint = hp.transform;
			}
		}
	}

	void Update() {
		AutoDetectReferences();
		UpdateTargetRaycast();
		HandleInput();
		HandleDistanceScroll();
		HandleObjectRotationInput();
	}

	void FixedUpdate() {
		if (IsHoldingObject) {
			UpdateHeldObjectPhysics();
		}
	}

	/// <summary>
	/// Casts a ray/sphere forward from the camera center to find interactive grabbables.
	/// </summary>
	private void UpdateTargetRaycast() {
		if (playerCamera == null) return;

		TargetedGrabbable = null;
		if (IsHoldingObject) return;

		Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
		RaycastHit hit;

		// SphereCast provides a generous, tactile cone of detection
		bool hasHit = Physics.SphereCast(ray, sphereCastRadius, out hit, reachDistance, interactionLayers, QueryTriggerInteraction.Ignore);
		if (!hasHit) {
			hasHit = Physics.Raycast(ray, out hit, reachDistance, interactionLayers, QueryTriggerInteraction.Ignore);
		}

		if (hasHit) {
			// Ignore player's own colliders
			if (playerCharacter != null && (hit.collider == playerCharacter || hit.transform == transform || hit.transform.IsChildOf(transform))) {
				return;
			}

			Grabbable grabbable = hit.collider.GetComponentInParent<Grabbable>();
			if (grabbable == null) {
				// Check if hit object has a Rigidbody, and dynamically make it grabbable
				Rigidbody rb = hit.collider.attachedRigidbody;
				if (rb != null && !rb.isKinematic) {
					grabbable = rb.gameObject.GetComponent<Grabbable>();
					if (grabbable == null) {
						grabbable = rb.gameObject.AddComponent<Grabbable>();
					}
				}
			}

			if (grabbable != null && !grabbable.IsHeld) {
				TargetedGrabbable = grabbable;
			}
		}
	}

	private void HandleInput() {
		bool grabPressedThisFrame = ReadGrabDown();
		bool grabReleasedThisFrame = ReadGrabUp();
		bool throwPressedThisFrame = ReadThrowDown();

		// Grabbing / Dropping
		if (!holdToGrab) {
			// Toggle mode
			if (grabPressedThisFrame) {
				if (IsHoldingObject) {
					DropObject();
				} else if (TargetedGrabbable != null) {
					GrabObject(TargetedGrabbable);
				}
			}
		} else {
			// Hold to grab mode
			if (grabPressedThisFrame && TargetedGrabbable != null && !IsHoldingObject) {
				GrabObject(TargetedGrabbable);
			} else if (grabReleasedThisFrame && IsHoldingObject) {
				DropObject();
			}
		}

		// Throwing
		if (IsHoldingObject && throwPressedThisFrame && !IsRotatingHeldObject) {
			ThrowHeldObject();
		}
	}

	private void HandleDistanceScroll() {
		if (!IsHoldingObject || playerCamera == null) return;

		float distDelta = 0f;
		if (ReadKeyHeld(pushFartherKey)) distDelta += keyDistanceSpeed * Time.deltaTime;
		if (ReadKeyHeld(pullCloserKey)) distDelta -= keyDistanceSpeed * Time.deltaTime;

		if (allowMouseScrollDistance) {
			float scroll = ReadScrollDelta();
			if (Mathf.Abs(scroll) > 0.01f) {
				distDelta += scroll * scrollSensitivity;
			}
		}

		if (Mathf.Abs(distDelta) > 0.0001f) {
			CurrentHoldDistance += distDelta;
			CurrentHoldDistance = Mathf.Clamp(CurrentHoldDistance, minHoldDistance, maxHoldDistance);
		}
	}

	private void HandleObjectRotationInput() {
		if (!IsHoldingObject || playerCamera == null) {
			IsRotatingHeldObject = false;
			return;
		}

		bool rotatingViaKeys = false;
		Vector2 keyRotation = Vector2.zero;
		if (ReadKeyHeld(rotatePitchUpKey)) keyRotation.y += 1f;
		if (ReadKeyHeld(rotatePitchDownKey)) keyRotation.y -= 1f;
		if (ReadKeyHeld(rotateYawLeftKey)) keyRotation.x -= 1f;
		if (ReadKeyHeld(rotateYawRightKey)) keyRotation.x += 1f;

		if (keyRotation.sqrMagnitude > 0.01f) {
			rotatingViaKeys = true;
			Quaternion rotX = Quaternion.AngleAxis(keyRotation.x * keyRotateSpeed * Time.deltaTime, playerCamera.transform.up);
			Quaternion rotY = Quaternion.AngleAxis(keyRotation.y * keyRotateSpeed * Time.deltaTime, playerCamera.transform.right);
			userRotationOffset = rotX * rotY * userRotationOffset;
		}

		bool mouseRotateHeld = allowMouseRotate && ReadRotateHeld();
		if (mouseRotateHeld) {
			Vector2 mouseDelta = ReadMouseDelta();
			if (mouseDelta.sqrMagnitude > 0.001f) {
				Quaternion rotX = Quaternion.AngleAxis(-mouseDelta.x * rotateSensitivity, playerCamera.transform.up);
				Quaternion rotY = Quaternion.AngleAxis(mouseDelta.y * rotateSensitivity, playerCamera.transform.right);
				userRotationOffset = rotX * rotY * userRotationOffset;
			}
		}

		IsRotatingHeldObject = rotatingViaKeys || mouseRotateHeld;
	}

	public void GrabObject(Grabbable grabbable) {
		if (grabbable == null || grabbable.Rb == null) return;
		if (IsHoldingObject) DropObject();

		HeldGrabbable = grabbable;
		CurrentHoldDistance = Mathf.Clamp(grabbable.holdDistance, minHoldDistance, maxHoldDistance);
		userRotationOffset = Quaternion.identity;

		// Ignore collision between held object colliders and player character controller
		heldColliders = HeldGrabbable.GetComponentsInChildren<Collider>();
		if (playerCharacter != null && heldColliders != null) {
			for (int i = 0; i < heldColliders.Length; i++) {
				var col = heldColliders[i];
				if (col != null && !col.isTrigger) {
					Physics.IgnoreCollision(playerCharacter, col, true);
				}
			}
		}

		HeldGrabbable.OnGrab(this, holdPoint);
	}

	public void DropObject() {
		if (!IsHoldingObject) return;

		Grabbable dropped = HeldGrabbable;
		HeldGrabbable = null;
		IsRotatingHeldObject = false;

		// Restore collision
		if (playerCharacter != null && heldColliders != null) {
			for (int i = 0; i < heldColliders.Length; i++) {
				var col = heldColliders[i];
				if (col != null && !col.isTrigger) {
					Physics.IgnoreCollision(playerCharacter, col, false);
				}
			}
		}
		heldColliders = null;

		dropped.OnDrop();
	}

	public void ThrowHeldObject() {
		if (!IsHoldingObject || playerCamera == null) return;

		Grabbable thrown = HeldGrabbable;
		HeldGrabbable = null;
		IsRotatingHeldObject = false;

		// Restore collision
		if (playerCharacter != null && heldColliders != null) {
			for (int i = 0; i < heldColliders.Length; i++) {
				var col = heldColliders[i];
				if (col != null && !col.isTrigger) {
					Physics.IgnoreCollision(playerCharacter, col, false);
				}
			}
		}
		heldColliders = null;

		Vector3 force = (playerCamera.transform.forward * throwForce) + (Vector3.up * throwUpwardBias);
		Vector3 randomTorque = Random.insideUnitSphere * 5f;
		thrown.OnThrow(force, randomTorque);
	}

	private void UpdateHeldObjectPhysics() {
		if (HeldGrabbable == null || HeldGrabbable.Rb == null || playerCamera == null) {
			DropObject();
			return;
		}

		Rigidbody rb = HeldGrabbable.Rb;
		Vector3 targetHoldPosition = playerCamera.transform.position + (playerCamera.transform.forward * CurrentHoldDistance);

		// Distance break check
		float distToTarget = Vector3.Distance(rb.position, targetHoldPosition);
		if (distToTarget > breakDistance) {
			DropObject();
			return;
		}

		// Smooth physics velocity pull
		Vector3 direction = (targetHoldPosition - rb.position);
		Vector3 targetVelocity = direction * followSpeed;
		targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxHoldVelocity);
		Grabbable.SetLinearVelocity(rb, targetVelocity);

		// Smooth rotation matching
		Quaternion targetRot = playerCamera.transform.rotation * userRotationOffset * Quaternion.Euler(HeldGrabbable.holdRotationOffset);
		rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * rotationSpeed));
		Grabbable.SetAngularVelocity(rb, Vector3.zero);
	}

	// Input helper methods supporting both Input System & Legacy Input
	public bool ReadKeyDown(KeyCode kc) {
#if ENABLE_INPUT_SYSTEM
		var kb = Keyboard.current;
		if (kb != null) {
			if (System.Enum.TryParse<Key>(kc.ToString(), true, out var key)) {
				try { if (kb[key].wasPressedThisFrame) return true; } catch {}
			}
		}
#endif
		try {
			if (Input.GetKeyDown(kc)) return true;
		} catch {}
		return false;
	}

	public bool ReadKeyHeld(KeyCode kc) {
#if ENABLE_INPUT_SYSTEM
		var kb = Keyboard.current;
		if (kb != null) {
			if (System.Enum.TryParse<Key>(kc.ToString(), true, out var key)) {
				try { if (kb[key].isPressed) return true; } catch {}
			}
		}
#endif
		try {
			if (Input.GetKey(kc)) return true;
		} catch {}
		return false;
	}

	public bool ReadKeyUp(KeyCode kc) {
#if ENABLE_INPUT_SYSTEM
		var kb = Keyboard.current;
		if (kb != null) {
			if (System.Enum.TryParse<Key>(kc.ToString(), true, out var key)) {
				try { if (kb[key].wasReleasedThisFrame) return true; } catch {}
			}
		}
#endif
		try {
			if (Input.GetKeyUp(kc)) return true;
		} catch {}
		return false;
	}

	private bool ReadGrabDown() {
		return ReadKeyDown(grabKey);
	}

	private bool ReadGrabUp() {
		return ReadKeyUp(grabKey);
	}

	private bool ReadThrowDown() {
		if (ReadKeyDown(throwKey)) return true;
		if (allowMouseThrow) {
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
			try {
				if (Input.GetMouseButtonDown(0)) return true;
			} catch {}
		}
		return false;
	}

	private bool ReadRotateHeld() {
#if ENABLE_INPUT_SYSTEM
		if (Mouse.current != null && Mouse.current.rightButton.isPressed) return true;
#endif
		try {
			if (Input.GetMouseButton(1)) return true;
		} catch {}
		return false;
	}

	private float ReadScrollDelta() {
		float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
		if (Mouse.current != null) {
			scroll = Mouse.current.scroll.ReadValue().y * 0.01f;
		}
#endif
		if (scroll == 0f) {
			try {
				scroll = Input.GetAxis("Mouse ScrollWheel");
			} catch {}
		}
		return scroll;
	}

	private Vector2 ReadMouseDelta() {
		Vector2 delta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
		if (Mouse.current != null) {
			delta = Mouse.current.delta.ReadValue() * 0.1f;
		}
#endif
		if (delta == Vector2.zero) {
			try {
				delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
			} catch {}
		}
		return delta;
	}
}
