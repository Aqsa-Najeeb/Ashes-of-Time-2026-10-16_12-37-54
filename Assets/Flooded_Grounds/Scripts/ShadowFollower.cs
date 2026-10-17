using UnityEngine;

public class ShadowFollower : MonoBehaviour {
	public Transform targetPlayer;
	public float followDistance = 7.0f;
	public float moveSpeed = 3.5f;
	public float hoverHeight = 1.6f;
	public float bobSpeed = 2.0f;
	public float bobAmount = 0.25f;
	private Camera playerCam;

	void Start() {
		if (targetPlayer == null) {
			var motor = Object.FindAnyObjectByType<CharController_Motor>();
			if (motor != null) targetPlayer = motor.transform;
		}
		if (targetPlayer != null) {
			playerCam = targetPlayer.GetComponentInChildren<Camera>();
		}
	}

	void Update() {
		if (targetPlayer == null) return;

		// Desired position is behind player
		Vector3 behindPlayer = targetPlayer.position - (targetPlayer.forward * followDistance);

		// Match ground height if terrain exists
		float groundY = targetPlayer.position.y;
		var terrain = Terrain.activeTerrain;
		if (terrain != null) {
			groundY = terrain.SampleHeight(behindPlayer);
		}
		float targetY = Mathf.Max(groundY + hoverHeight, targetPlayer.position.y);
		targetY += Mathf.Sin(Time.time * bobSpeed) * bobAmount;
		Vector3 targetPos = new Vector3(behindPlayer.x, targetY, behindPlayer.z);

		// Check if player is directly looking at the shadow
		bool beingWatched = false;
		if (playerCam != null) {
			Vector3 toShadow = (transform.position - playerCam.transform.position).normalized;
			float lookDot = Vector3.Dot(playerCam.transform.forward, toShadow);
			if (lookDot > 0.55f) beingWatched = true;
		}

		// If watched, freeze or retreat slowly into shadows; otherwise stalk forward
		float currentSpeed = beingWatched ? (moveSpeed * 0.2f) : moveSpeed;

		// Thematic interaction: Fire repels Shadow
		FireSanctuary protectingFire;
		bool playerInFire = FireSanctuary.IsPositionProtected(targetPlayer.position, out protectingFire);
		bool shadowInFire = FireSanctuary.IsPositionProtected(transform.position, out _);

		if (playerInFire && protectingFire != null) {
			// Player is safe inside fire sanctuary; shadow lingers outside the fire rim
			Vector3 fireToShadow = (transform.position - protectingFire.transform.position);
			fireToShadow.y = 0f;
			if (fireToShadow.sqrMagnitude < 0.01f) fireToShadow = -targetPlayer.forward;
			fireToShadow.Normalize();
			targetPos = protectingFire.transform.position + fireToShadow * (protectingFire.protectionRadius + 2.5f);
			targetPos.y = Mathf.Max(groundY + hoverHeight, targetPlayer.position.y);
			currentSpeed = moveSpeed * 0.6f;
		} else if (shadowInFire) {
			// Shadow accidentally touched fire light; recoil back into the shadows
			var nearestFire = FireSanctuary.GetClosestFire(transform.position);
			if (nearestFire != null) {
				Vector3 repelDir = (transform.position - nearestFire.transform.position);
				repelDir.y = 0f;
				repelDir.Normalize();
				targetPos = nearestFire.transform.position + repelDir * (nearestFire.protectionRadius + 3.5f);
				targetPos.y = Mathf.Max(groundY + hoverHeight, targetPlayer.position.y);
			}
		}

		transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * currentSpeed);

		// Face the player
		Vector3 lookDir = targetPlayer.position - transform.position;
		lookDir.y = 0f;
		if (lookDir.sqrMagnitude > 0.01f) {
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
		}
	}
}
