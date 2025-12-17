using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShakeButtonCoroutine : MonoBehaviour
{
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 5f;

    private Button button;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private bool isShaking = false;

    private void Start()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        button.onClick.AddListener(StartShake);
    }

    private void StartShake()
    {
        if (!isShaking)
        {
            StartCoroutine(Shake());
        }
    }

    private IEnumerator Shake()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            Vector2 randomOffset = Random.insideUnitCircle * shakeIntensity;
            rectTransform.anchoredPosition = originalPosition + (Vector3)randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        isShaking = false;
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(StartShake);
    }
}