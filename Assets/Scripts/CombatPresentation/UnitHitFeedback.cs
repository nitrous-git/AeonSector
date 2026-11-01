using UnityEngine;

// Only the art child moves; board position, colliders, health UI and facing are untouched.
public sealed class UnitHitFeedback : MonoBehaviour
{
    public CombatVfxProfile profile;
    public SpriteRenderer body;
    public Material flashMaterial;
    private SpriteRenderer flash;
    private Vector3 rest, kick;
    private float remaining;

    private void Awake()
    {
        if (body == null) return;
        rest = body.transform.localPosition;
        flash = new GameObject("Hit flash").AddComponent<SpriteRenderer>();
        flash.transform.SetParent(body.transform, false);
        flash.sharedMaterial = flashMaterial;
        flash.sortingLayerID = body.sortingLayerID;
        flash.sortingOrder = body.sortingOrder + 1;
        flash.enabled = false;
    }

    public void Hit(Vector3 direction)
    {
        if (body == null || flash == null) return;
        // Restart without accumulating displacement on overlapping artillery hits.
        kick = transform.InverseTransformVector(direction.normalized * 0.075f);
        remaining = 0.18f;
    }

    private void LateUpdate()
    {
        if (remaining <= 0f || body == null || flash == null) return;
        remaining = Mathf.Max(0, remaining - Time.deltaTime);
        float strength = remaining / 0.18f;
        if (body.transform != transform) body.transform.localPosition = rest + kick * strength * strength;
        flash.sprite = body.sprite;
        flash.flipX = body.flipX; flash.flipY = body.flipY;
        flash.color = new Color(0.85f, 0.97f, 1f, Mathf.Clamp01((remaining - 0.07f) / 0.11f) * 0.85f);
        flash.enabled = remaining > 0.07f;
    }

    private void OnDisable()
    {
        remaining = 0;
        if (body != null && body.transform != transform) body.transform.localPosition = rest;
        if (flash != null) flash.enabled = false;
    }
}
