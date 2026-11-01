using TMPro;
using UnityEngine;

public sealed class FloatingDamageNumber : MonoBehaviour
{
    public TMP_Text label;
    public float lifetime = 0.85f;
    private float elapsed;
    private Vector3 start;
    private Vector3 fittedScale;

    public void Show(int amount, bool lethal, bool playerTarget)
    {
        start = transform.position;
        label.text = lethal ? $"-{amount}\n<size=45%>DESTROYED</size>" : $"-{amount}";
        label.color = playerTarget ? new Color(1f, 0.6f, 0.5f) : new Color(0.82f, 0.97f, 1f);
        label.raycastTarget = false;
        // Fit to world-space height, independent of the font's point-size metrics.
        label.ForceMeshUpdate();
        float height = Mathf.Max(0.01f, label.textBounds.size.y);
        fittedScale = Vector3.one * ((lethal ? 0.48f : 0.30f) / height);
        transform.localScale = fittedScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }
        transform.position = start + Vector3.up * (0.5f * (1f - Mathf.Pow(1f - t, 2)));
        transform.localScale = fittedScale * (1f + 0.22f * Mathf.Exp(-t * 12f));
        label.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, t));
    }
}
