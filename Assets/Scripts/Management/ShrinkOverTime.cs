using UnityEngine;

public class ShrinkOverTime : MonoBehaviour
{
    [Header("Shrink Settings")]
    public float shrinkDuration = 2f; 
    public float minScale = 0.2f;     

    private Vector3 originalScale;
    private float elapsed;
    public bool isShrinking = true;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (!isShrinking) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / shrinkDuration);
        float scale = Mathf.Lerp(1f, minScale, t);
        transform.localScale = originalScale * scale;

        if (t >= 1f)
        {
            Destroy(gameObject); 
        }
    }

    public float GetCurrentSizeRatio()
    {
        return transform.localScale.x / originalScale.x;
    }
}
