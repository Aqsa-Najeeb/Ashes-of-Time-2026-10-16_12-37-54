using System.Collections;
using UnityEngine;

/// <summary>
/// Component attached to any object that can be grabbed, held, and thrown by the player.
/// Automatically handles physics state transitions, colliders, and tactile audio feedback.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour {
	[Header("Display & Identification")]
	[Tooltip("Display name shown on the HUD prompt (e.g., 'Ancient Lantern'). If blank, cleans up gameObject.name.")]
	public string objectDisplayName = "";

	[Header("Holding Parameters")]
	[Tooltip("Target distance in front of the camera when held.")]
	public float holdDistance = 1.85f;
	[Tooltip("Local rotation offset relative to the camera when held.")]
	public Vector3 holdRotationOffset = Vector3.zero;
	[Tooltip("Whether the object should rotate to align with camera orientation.")]
	public bool alignRotationWithCamera = true;
	[Tooltip("Additional mass override when held (0 keeps default).")]
	public float heldMass = 0f;

	[Header("Throwing")]
	public bool canBeThrown = true;
	[Range(0.5f, 3.0f)]
	public float throwForceMultiplier = 1.0f;
	public float throwTorque = 4.0f;

	[Header("Audio Feedback (Optional Custom Clips)")]
	public AudioClip customGrabClip;
	public AudioClip customDropClip;
	public AudioClip customThrowClip;
	public AudioClip customImpactClip;
	public float audioVolume = 0.65f;

	// Runtime state
	public bool IsHeld { get; private set; }
	public PlayerGrabController CurrentHolder { get; private set; }
	public Transform CurrentHoldAnchor { get; private set; }
	public Rigidbody Rb { get; private set; }

	private bool initialUseGravity = true;
	private bool initialIsKinematic = false;
	private float initialLinearDamping = 0.05f;
	private float initialAngularDamping = 0.05f;
	private RigidbodyInterpolation initialInterpolation;
	private CollisionDetectionMode initialCollisionMode;
	private float initialMass = 1.0f;
	private AudioSource audioSource;
	private float lastImpactTime = 0f;

	void Awake() {
		Rb = GetComponent<Rigidbody>();
		if (Rb == null) {
			Rb = gameObject.AddComponent<Rigidbody>();
			Rb.mass = 2.0f;
		}

		initialMass = Rb.mass;
		initialUseGravity = Rb.useGravity;
		initialIsKinematic = Rb.isKinematic;
		initialInterpolation = Rb.interpolation;
		initialCollisionMode = Rb.collisionDetectionMode;

#if UNITY_6000_0_OR_NEWER
		initialLinearDamping = Rb.linearDamping;
		initialAngularDamping = Rb.angularDamping;
#else
		initialLinearDamping = Rb.drag;
		initialAngularDamping = Rb.angularDrag;
#endif

		// Ensure collider exists
		var colliders = GetComponentsInChildren<Collider>();
		if (colliders == null || colliders.Length == 0) {
			var box = gameObject.AddComponent<BoxCollider>();
			var mf = GetComponentInChildren<MeshFilter>();
			if (mf != null && mf.sharedMesh != null) {
				box.size = mf.sharedMesh.bounds.size;
				box.center = mf.sharedMesh.bounds.center;
			}
		}

		// Ensure audio source
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null) {
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0.85f; // mostly 3D
			audioSource.minDistance = 1.0f;
			audioSource.maxDistance = 20.0f;
		}
	}

	/// <summary>
	/// Returns the clean formatted name for the HUD tooltip.
	/// </summary>
	public string GetDisplayName() {
		if (!string.IsNullOrEmpty(objectDisplayName)) {
			return objectDisplayName;
		}

		string n = gameObject.name;
		n = n.Replace("(Clone)", "").Trim();
		if (n.StartsWith("Prop_", System.StringComparison.OrdinalIgnoreCase)) n = n.Substring(5);
		if (n.StartsWith("SM_", System.StringComparison.OrdinalIgnoreCase)) n = n.Substring(3);
		if (n.StartsWith("VFX_", System.StringComparison.OrdinalIgnoreCase)) n = n.Substring(4);
		if (n.StartsWith("lo_", System.StringComparison.OrdinalIgnoreCase)) n = n.Substring(3);

		// Format underscores and CamelCase to spaced words
		n = n.Replace('_', ' ');
		// Clean trailing numbers like " (1)" or "1"
		n = System.Text.RegularExpressions.Regex.Replace(n, @"\s*\(\d+\)", "");
		n = System.Text.RegularExpressions.Regex.Replace(n, @"(?<=[a-zA-Z])(?=\d+$)", " ");
		n = System.Text.RegularExpressions.Regex.Replace(n, @"(?<!^)(?=[A-Z])", " ");

		n = n.Trim();
		if (string.IsNullOrEmpty(n)) n = "Object";
		return n;
	}

	/// <summary>
	/// Called when the player grabs this object.
	/// </summary>
	public virtual void OnGrab(PlayerGrabController holder, Transform holdAnchor) {
		IsHeld = true;
		CurrentHolder = holder;
		CurrentHoldAnchor = holdAnchor;

		if (Rb != null) {
			Rb.useGravity = false;
			Rb.isKinematic = false;
			Rb.interpolation = RigidbodyInterpolation.Interpolate;
			Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

#if UNITY_6000_0_OR_NEWER
			Rb.linearDamping = 6.0f;
			Rb.angularDamping = 8.0f;
#else
			Rb.drag = 6.0f;
			Rb.angularDrag = 8.0f;
#endif
			if (heldMass > 0f) Rb.mass = heldMass;
		}

		PlaySound(customGrabClip != null ? customGrabClip : ProceduralAudioHelper.GetGrabClip());
	}

	/// <summary>
	/// Called when the player drops this object.
	/// </summary>
	public virtual void OnDrop() {
		IsHeld = false;
		CurrentHolder = null;
		CurrentHoldAnchor = null;

		if (Rb != null) {
			Rb.useGravity = initialUseGravity;
			Rb.isKinematic = initialIsKinematic;
			Rb.interpolation = initialInterpolation;
			Rb.collisionDetectionMode = initialCollisionMode;
			Rb.mass = initialMass;

#if UNITY_6000_0_OR_NEWER
			Rb.linearDamping = initialLinearDamping;
			Rb.angularDamping = initialAngularDamping;
#else
			Rb.drag = initialLinearDamping;
			Rb.angularDrag = initialAngularDamping;
#endif
		}

		PlaySound(customDropClip != null ? customDropClip : ProceduralAudioHelper.GetDropClip());
	}

	/// <summary>
	/// Called when the player throws this object forward.
	/// </summary>
	public virtual void OnThrow(Vector3 force, Vector3 torque) {
		OnDrop();
		if (Rb != null) {
			SetLinearVelocity(Rb, Vector3.zero);
			Rb.AddForce(force * throwForceMultiplier, ForceMode.Impulse);
			Rb.AddTorque(torque * throwTorque, ForceMode.Impulse);
		}
		PlaySound(customThrowClip != null ? customThrowClip : ProceduralAudioHelper.GetThrowClip());
	}

	public static void SetLinearVelocity(Rigidbody rb, Vector3 vel) {
#if UNITY_6000_0_OR_NEWER
		rb.linearVelocity = vel;
#else
		rb.velocity = vel;
#endif
	}

	public static Vector3 GetLinearVelocity(Rigidbody rb) {
#if UNITY_6000_0_OR_NEWER
		return rb.linearVelocity;
#else
		return rb.velocity;
#endif
	}

	public static void SetAngularVelocity(Rigidbody rb, Vector3 aVel) {
		rb.angularVelocity = aVel;
	}

	private void PlaySound(AudioClip clip) {
		if (clip == null) return;
		if (audioSource != null) {
			audioSource.pitch = Random.Range(0.95f, 1.05f);
			audioSource.PlayOneShot(clip, audioVolume);
		} else {
			AudioSource.PlayClipAtPoint(clip, transform.position, audioVolume);
		}
	}

	void OnCollisionEnter(Collision collision) {
		if (IsHeld) return;
		if (Time.time - lastImpactTime < 0.18f) return;

		float speed = collision.relativeVelocity.magnitude;
		if (speed > 1.6f) {
			lastImpactTime = Time.time;
			float volume = Mathf.Clamp01((speed - 1.6f) / 6f) * audioVolume;
			PlaySound(customImpactClip != null ? customImpactClip : ProceduralAudioHelper.GetImpactClip());
		}
	}
}
