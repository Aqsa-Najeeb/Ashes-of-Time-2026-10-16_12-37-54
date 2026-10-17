using UnityEngine;

/// <summary>
/// Generates lightweight, procedural sound effects for grabbing, dropping, throwing, and impacts.
/// Eliminates dependency on external audio assets while delivering crisp tactile feedback.
/// </summary>
public static class ProceduralAudioHelper {
	private static AudioClip grabClip;
	private static AudioClip dropClip;
	private static AudioClip throwClip;
	private static AudioClip impactClip;

	public static AudioClip GetGrabClip() {
		if (grabClip == null) {
			grabClip = CreateToneClip("Grab_SFX", 0.08f, 520f, 320f, 0.45f, true);
		}
		return grabClip;
	}

	public static AudioClip GetDropClip() {
		if (dropClip == null) {
			dropClip = CreateToneClip("Drop_SFX", 0.12f, 260f, 140f, 0.35f, false);
		}
		return dropClip;
	}

	public static AudioClip GetThrowClip() {
		if (throwClip == null) {
			throwClip = CreateNoiseWhooshClip("Throw_SFX", 0.22f, 0.5f);
		}
		return throwClip;
	}

	public static AudioClip GetImpactClip() {
		if (impactClip == null) {
			impactClip = CreateImpactClip("Impact_SFX", 0.15f, 180f, 0.4f);
		}
		return impactClip;
	}

	private static AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq, float volume, bool snappy) {
		int sampleRate = 44100;
		int totalSamples = Mathf.Max(1, (int)(sampleRate * duration));
		float[] samples = new float[totalSamples];

		float phase = 0f;
		for (int i = 0; i < totalSamples; i++) {
			float t = (float)i / totalSamples;
			float currentFreq = Mathf.Lerp(startFreq, endFreq, snappy ? Mathf.Pow(t, 2f) : t);
			phase += 2f * Mathf.PI * currentFreq / sampleRate;

			float envelope = Mathf.Exp(-t * (snappy ? 16f : 8f));
			float wave = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.3f;
			samples[i] = wave * envelope * volume;
		}

		AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}

	private static AudioClip CreateNoiseWhooshClip(string name, float duration, float volume) {
		int sampleRate = 44100;
		int totalSamples = Mathf.Max(1, (int)(sampleRate * duration));
		float[] samples = new float[totalSamples];

		float prevNoise = 0f;
		for (int i = 0; i < totalSamples; i++) {
			float t = (float)i / totalSamples;
			// Bell-shaped curve for whoosh
			float envelope = Mathf.Sin(t * Mathf.PI);
			envelope = Mathf.Pow(envelope, 1.8f);

			float whiteNoise = (Random.value * 2f - 1f);
			// Soft low-pass filter
			prevNoise = Mathf.Lerp(prevNoise, whiteNoise, 0.15f);
			samples[i] = prevNoise * envelope * volume;
		}

		AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}

	private static AudioClip CreateImpactClip(string name, float duration, float baseFreq, float volume) {
		int sampleRate = 44100;
		int totalSamples = Mathf.Max(1, (int)(sampleRate * duration));
		float[] samples = new float[totalSamples];

		float phase = 0f;
		for (int i = 0; i < totalSamples; i++) {
			float t = (float)i / totalSamples;
			float currentFreq = Mathf.Lerp(baseFreq, 60f, t);
			phase += 2f * Mathf.PI * currentFreq / sampleRate;

			float decay = Mathf.Exp(-t * 22f);
			float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 35f) * 0.4f;
			float wave = Mathf.Sin(phase) * decay * 0.6f;
			samples[i] = (wave + noise) * volume;
		}

		AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}
}
