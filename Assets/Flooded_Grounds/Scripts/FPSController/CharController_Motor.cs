using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CharController_Motor : MonoBehaviour {
	[Header("Movement Settings")]
	public float speed = 10.0f;
	public float sprintMultiplier = 1.6f;
	public float jumpForce = 6.0f;
	public float gravity = -9.8f;
	public float WaterHeight = 15.5f;

	[Header("Look Settings")]
	public float sensitivity = 100.0f;
	public GameObject cam;
	public bool lockCursorOnStart = true;

	private CharacterController character;
	private PlayerGrabController grabController;
	private float verticalVelocity = 0f;
	private float verticalRotation = 0f;

	void Start() {
		character = GetComponent<CharacterController>();
		grabController = GetComponent<PlayerGrabController>();
		if (grabController == null) {
			grabController = gameObject.AddComponent<PlayerGrabController>();
		}
		if (GetComponent<GrabbableHUD>() == null) {
			gameObject.AddComponent<GrabbableHUD>();
		}
		if (cam == null) {
			Camera c = GetComponentInChildren<Camera>();
			if (c != null) cam = c.gameObject;
		}
		if (cam != null) {
			verticalRotation = cam.transform.localEulerAngles.x;
			if (verticalRotation > 180f) verticalRotation -= 360f;
		}
		if (lockCursorOnStart) {
			SetCursorLock(true);
		}
	}

	public void SetCursorLock(bool locked) {
		Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !locked;
	}

	void Update() {
		HandleCursor();
		Vector2 moveInput = ReadMoveInput();
		bool isSprinting = ReadSprintInput();
		bool jumpPressed = ReadJumpInput();

		float currentSpeed = speed * (isSprinting ? sprintMultiplier : 1.0f);
		Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x) * currentSpeed;

		bool inWater = transform.position.y < WaterHeight;
		if (inWater) {
			verticalVelocity = 0f;
			if (jumpPressed) {
				verticalVelocity = jumpForce * 0.7f;
			} else if (ReadCrouchInput()) {
				verticalVelocity = -jumpForce * 0.7f;
			}
		} else {
			if (character != null && character.isGrounded) {
				if (verticalVelocity < 0f) {
					verticalVelocity = -2f;
				}
				if (jumpPressed) {
					verticalVelocity = jumpForce;
				}
			} else {
				verticalVelocity += gravity * Time.deltaTime;
			}
		}

		Vector3 totalMovement = moveDirection + new Vector3(0f, verticalVelocity, 0f);
		if (character != null && character.enabled) {
			character.Move(totalMovement * Time.deltaTime);
		}

		HandleLook();
	}

	Vector2 ReadMoveInput() {
		float h = 0f;
		float v = 0f;
		#if ENABLE_INPUT_SYSTEM
		var kb = Keyboard.current;
		if (kb != null) {
			if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
			if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
			if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
			if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
		}
		#endif
		try {
			if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
			if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
			if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
			if (h == 0f) h = Input.GetAxisRaw("Horizontal");
			if (v == 0f) v = Input.GetAxisRaw("Vertical");
		} catch {}
		h = Mathf.Clamp(h, -1f, 1f);
		v = Mathf.Clamp(v, -1f, 1f);
		Vector2 input = new Vector2(h, v);
		if (input.sqrMagnitude > 1f) {
			input.Normalize();
		}
		return input;
	}

	bool ReadSprintInput() {
		#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)) return true;
		#endif
		try {
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
		} catch {}
		return false;
	}

	bool ReadJumpInput() {
		#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
		#endif
		try {
			if (Input.GetKeyDown(KeyCode.Space)) return true;
		} catch {}
		return false;
	}

	bool ReadCrouchInput() {
		#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed)) return true;
		#endif
		try {
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) return true;
		} catch {}
		return false;
	}

	void HandleLook() {
		if (Cursor.lockState != CursorLockMode.Locked) return;
		if (grabController != null && grabController.IsRotatingHeldObject) return;
		float mouseX = 0f;
		float mouseY = 0f;
		#if ENABLE_INPUT_SYSTEM
		var mouse = Mouse.current;
		if (mouse != null) {
			Vector2 delta = mouse.delta.ReadValue();
			mouseX = delta.x * (sensitivity * 0.05f);
			mouseY = delta.y * (sensitivity * 0.05f);
		}
		#endif
		if (mouseX == 0f && mouseY == 0f) {
			try {
				mouseX = Input.GetAxis("Mouse X") * sensitivity * 0.5f;
				mouseY = Input.GetAxis("Mouse Y") * sensitivity * 0.5f;
			} catch {}
		}
		transform.Rotate(0f, mouseX * Time.deltaTime * 60f, 0f);
		if (cam != null) {
			verticalRotation -= mouseY * Time.deltaTime * 60f;
			verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f);
			cam.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
		}
	}

	void HandleCursor() {
		#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetCursorLock(false);
		if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) SetCursorLock(true);
		#endif
		try {
			if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLock(false);
			if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) SetCursorLock(true);
		} catch {}
	}
}
