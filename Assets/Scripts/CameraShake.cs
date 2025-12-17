using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraShake : MonoBehaviour
{
    [Header("Настройки тряски")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.05f;
    
    [Header("Ссылки")]
    [SerializeField] private List<Button> buttons;
    [SerializeField] private List<Canvas> canvases;
    
    private Vector3 originalCameraPos;
    private Dictionary<Canvas, Vector3> originalCanvasPositions = new Dictionary<Canvas, Vector3>();
    private bool isShaking = false;

    void Start()
    {
        originalCameraPos = transform.position;
        
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(() => StartCoroutine(ShakeAll()));
        }
        
        foreach (Canvas canvas in canvases)
        {
            originalCanvasPositions[canvas] = canvas.transform.position;
        }
    }

    IEnumerator ShakeAll()
    {
        if (isShaking) yield break;
        
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.position = originalCameraPos + new Vector3(x, y, 0f);

            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && originalCanvasPositions.ContainsKey(canvas))
                {
                    canvas.transform.position = originalCanvasPositions[canvas] + new Vector3(x, y, 0f);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalCameraPos;
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && originalCanvasPositions.ContainsKey(canvas))
            {
                canvas.transform.position = originalCanvasPositions[canvas];
            }
        }

        isShaking = false;
    }
}