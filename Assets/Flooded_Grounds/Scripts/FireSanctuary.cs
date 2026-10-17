using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a fire or torch as a sanctuary zone.
/// Connects the Fire theme with the Shadow theme:
/// The Shadow Stalker is repelled by fire light and will not enter the protection radius.
/// </summary>
public class FireSanctuary : MonoBehaviour {
	public static readonly List<FireSanctuary> ActiveFires = new List<FireSanctuary>();

	[Header("Sanctuary Zone")]
	[Tooltip("Radius around the fire where the player is protected from shadows.")]
	public float protectionRadius = 9.0f;
	[Tooltip("Strength with which the shadow stalker is pushed back.")]
	public float shadowRepelForce = 5.0f;

	[Header("Audio")]
	[Tooltip("Optional 3D crackle audio.")]
	public AudioSource fireAudio;

	void OnEnable() {
		if (!ActiveFires.Contains(this)) {
			ActiveFires.Add(this);
		}
	}

	void OnDisable() {
		ActiveFires.Remove(this);
	}

	void Awake() {
		if (fireAudio == null) {
			fireAudio = GetComponentInChildren<AudioSource>();
		}
		if (fireAudio != null) {
			fireAudio.spatialBlend = 1.0f; // full 3D sound
			fireAudio.minDistance = 2.0f;
			fireAudio.maxDistance = Mathf.Max(protectionRadius * 2f, 15f);
			fireAudio.rolloffMode = AudioRolloffMode.Linear;
			fireAudio.loop = true;
			if (!fireAudio.isPlaying && fireAudio.clip != null) {
				fireAudio.Play();
			}
		}
	}

	/// <summary>
	/// Returns the closest active fire sanctuary to the given position.
	/// </summary>
	public static FireSanctuary GetClosestFire(Vector3 position) {
		FireSanctuary closest = null;
		float closestDistSqr = float.MaxValue;
		for (int i = 0; i < ActiveFires.Count; i++) {
			var fire = ActiveFires[i];
			if (fire == null) continue;
			float dSqr = (fire.transform.position - position).sqrMagnitude;
			if (dSqr < closestDistSqr) {
				closestDistSqr = dSqr;
				closest = fire;
			}
		}
		return closest;
	}

	/// <summary>
	/// Checks if a position is currently within any active fire sanctuary.
	/// </summary>
	public static bool IsPositionProtected(Vector3 position, out FireSanctuary protectingFire) {
		protectingFire = null;
		for (int i = 0; i < ActiveFires.Count; i++) {
			var fire = ActiveFires[i];
			if (fire == null) continue;
			float dist = Vector3.Distance(fire.transform.position, position);
			if (dist <= fire.protectionRadius) {
				protectingFire = fire;
				return true;
			}
		}
		return false;
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
		Gizmos.DrawWireSphere(transform.position, protectionRadius);
	}
}
