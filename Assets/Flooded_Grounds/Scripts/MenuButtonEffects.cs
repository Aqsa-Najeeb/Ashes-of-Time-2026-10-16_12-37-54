using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Provides visual scale feedback, hover sound, and click sound for menu buttons.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale Effect")]
    public float hoverScale = 1.04f;
    public float scaleSpeed = 12f;

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioSource audioSource;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Coroutine scaleCoroutine;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        targetScale = originalScale * hoverScale;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(targetScale));

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, 0.6f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(targetScale));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, 1.0f);
        }
    }

    private IEnumerator AnimateScale(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = target;
    }
}
