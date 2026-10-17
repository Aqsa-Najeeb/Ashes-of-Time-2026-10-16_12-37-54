using UnityEngine;

/// <summary>
/// Provides smooth, organic flame flickering for fire and candle lights using Perlin noise.
/// Avoids jarring random jumps while creating lively, dynamic shadows.
/// </summary>
[RequireComponent(typeof(Light))]
public class FireFlicker : MonoBehaviour {
	[Header("Intensity Flicker")]
	[Tooltip("Base intensity around which the light flickers. If 0, uses light's starting intensity.")]
	public float baseIntensity = 0f;
	[Tooltip("Percentage variation in intensity (e.g., 0.25 = +/- 25%).")]
	[Range(0.05f, 0.75f)]
	public float intensityVariation = 0.25f;
	[Tooltip("Speed of intensity fluctuations.")]
	public float flickerSpeed = 8.0f;

	[Header("Color Temperature Wobble")]
	public bool wobbleColor = true;
	[Tooltip("Deep ember orange flame.")]
	public Color warmColor = new Color(1.0f, 0.32f, 0.03f, 1.0f); // Real deep fire orange (#FF5208)
	[Tooltip("Vibrant golden-orange flame core.")]
	public Color brightColor = new Color(1.0f, 0.52f, 0.08f, 1.0f); // Warm vibrant flame orange (#FF8514)
	public float colorSpeed = 4.0f;

	[Header("Spatial Flame Wobble")]
	public bool wobblePosition = false;
	public float positionWobbleAmount = 0.04f;
	public float positionSpeed = 3.0f;

	private Light targetLight;
	private Vector3 originalLocalPos;
	private float noiseOffsetIntensity;
	private float noiseOffsetColor;
	private float noiseOffsetPos;

	void Awake() {
		targetLight = GetComponent<Light>();
		if (baseIntensity <= 0f && targetLight != null) {
			baseIntensity = targetLight.intensity;
		}
		originalLocalPos = transform.localPosition;

		// Random offsets so different lights in the scene do not flicker identically
		noiseOffsetIntensity = Random.Range(0f, 1000f);
		noiseOffsetColor = Random.Range(1000f, 2000f);
		noiseOffsetPos = Random.Range(2000f, 3000f);
	}

	void Update() {
		if (targetLight == null || !targetLight.enabled) return;

		// 1. Organic Intensity Flicker (Layered Perlin Noise)
		float timeI = (Time.time * flickerSpeed) + noiseOffsetIntensity;
		float noise1 = Mathf.PerlinNoise(timeI, 0.0f);
		float noise2 = Mathf.PerlinNoise(timeI * 2.3f, 15.7f) * 0.5f;
		float combinedNoise = (noise1 + noise2) / 1.5f; // 0.0 to 1.0

		float minInt = baseIntensity * (1.0f - intensityVariation);
		float maxInt = baseIntensity * (1.0f + intensityVariation);
		targetLight.intensity = Mathf.Lerp(minInt, maxInt, combinedNoise);

		// 2. Color Temperature Shift
		if (wobbleColor) {
			float timeC = (Time.time * colorSpeed) + noiseOffsetColor;
			float noiseC = Mathf.PerlinNoise(timeC, 5.0f);
			targetLight.color = Color.Lerp(warmColor, brightColor, noiseC);
		}

		// 3. Subtle spatial wobble
		if (wobblePosition) {
			float timeP = (Time.time * positionSpeed) + noiseOffsetPos;
			float x = (Mathf.PerlinNoise(timeP, 0f) - 0.5f) * positionWobbleAmount;
			float y = (Mathf.PerlinNoise(0f, timeP) - 0.5f) * (positionWobbleAmount * 0.5f);
			float z = (Mathf.PerlinNoise(timeP, timeP) - 0.5f) * positionWobbleAmount;
			transform.localPosition = originalLocalPos + new Vector3(x, y, z);
		}
	}

	public void SetBaseIntensity(float newBase) {
		baseIntensity = newBase;
		if (targetLight != null) targetLight.intensity = newBase;
	}
}
