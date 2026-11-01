using UnityEngine;

// Short, self-contained world-space accents. No gameplay callbacks or colliders.
public sealed class CombatEffect : MonoBehaviour
{
    public enum Shape { Charge, Muzzle, Slash, Impact, Blast, Destruction, Warning }
    public Shape shape;
    public float lifetime = 0.32f;
    public LineRenderer edgeLine;
    public LineRenderer energyLine;
    public LineRenderer coreLine;
    public ParticleSystem sparks;
    private Color energy, edge;
    private float elapsed, size = 1f;
    private Transform follow;
    private CombatUnit chargeOwner;
    private bool useOrigin;
    private SpriteRenderer ghost;
    private Vector3 ghostScale;

    public void Initialize(Color tint, Color outline, float scale, Vector3 direction,
        float duration = -1f, CombatUnit owner = null, bool atWeapon = false)
    {
        energy = tint; edge = outline; size = scale;
        if (duration > 0f) lifetime = duration;
        chargeOwner = owner; useOrigin = atWeapon;
        if (owner != null) follow = owner.transform;
        if (direction.sqrMagnitude > 0.001f && shape != Shape.Blast && shape != Shape.Warning && shape != Shape.Destruction)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        if (sparks != null)
        {
            var main = sparks.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(tint, Color.white, 0.7f), tint);
            main.startSizeMultiplier *= scale;
            main.startSpeedMultiplier *= scale;
            sparks.Play();
        }
        Render(0f);
    }

    public void SetGhost(SpriteRenderer source)
    {
        if (source == null || source.sprite == null) return;
        ghost = new GameObject("Collapsing silhouette").AddComponent<SpriteRenderer>();
        ghost.transform.SetParent(transform, true);
        ghost.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        ghost.transform.localScale = source.transform.lossyScale;
        ghostScale = ghost.transform.localScale;
        ghost.sprite = source.sprite;
        ghost.flipX = source.flipX; ghost.flipY = source.flipY;
        ghost.sharedMaterial = source.sharedMaterial;
        ghost.sortingLayerID = source.sortingLayerID;
        ghost.sortingOrder = source.sortingOrder;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime) { Destroy(gameObject); return; }
        if (shape == Shape.Charge && chargeOwner != null)
            transform.position = useOrigin ? chargeOwner.GetProjectileOrigin(chargeOwner.transform.position + Vector3.up * 0.6f)
                : CombatPresentation.HitPosition(chargeOwner);
        else if (shape == Shape.Charge && follow == null) { Destroy(gameObject); return; }
        Render(elapsed / lifetime);
    }

    private void Render(float t)
    {
        float fade = 1f - Mathf.SmoothStep(0.12f, 1f, t);
        float radius = size * Mathf.Lerp(0.12f, 0.53f, 1f - Mathf.Pow(1f - t, 3));
        float width = size * 0.055f * (1f - t * 0.8f);
        float start = 0, sweep = Mathf.PI * 2f, flatten = 1f;
        if (shape == Shape.Charge)
        {
            radius = size * Mathf.Lerp(0.32f, 0.06f, t);
            fade = Mathf.Sin(t * Mathf.PI) * 0.9f + 0.1f;
            start = t * 4f; sweep = Mathf.PI * 1.6f; width *= 0.65f;
        }
        else if (shape == Shape.Slash)
        {
            radius = size * Mathf.Lerp(0.45f, 0.9f, t);
            start = -1.35f + t * 0.45f; sweep = 2.7f; flatten = 0.8f;
            width = size * 0.11f * (1f - t);
        }
        else if (shape == Shape.Muzzle)
        {
            radius *= 0.65f; flatten = 0.55f; width *= 1.4f;
        }
        else if (shape == Shape.Blast || shape == Shape.Destruction)
        {
            radius *= 1.8f; flatten = 0.55f; width *= 1.3f;
        }
        else if (shape == Shape.Warning)
        {
            radius = size * (0.43f - 0.08f * t); flatten = 0.5f;
            width = 0.018f; fade = 0.5f + Mathf.Sin(t * Mathf.PI * 4f) * 0.2f;
        }
        Draw(edgeLine, radius, flatten, start, sweep, width * 2.5f, WithAlpha(edge, fade));
        Draw(energyLine, radius, flatten, start, sweep, width, WithAlpha(energy, fade));
        Draw(coreLine, radius * 0.99f, flatten, start, sweep, width * 0.3f, WithAlpha(Color.white, fade * 0.85f));
        if (ghost != null)
        {
            ghost.color = Color.Lerp(Color.white, new Color(0.07f, 0.1f, 0.14f, 0), Mathf.Clamp01(t * 2.5f));
            ghost.transform.localScale = ghostScale * Mathf.Lerp(1f, 0.75f, t);
        }
    }

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    private static void Draw(LineRenderer line, float radius, float flatten, float start, float sweep, float width, Color color)
    {
        if (line == null) return;
        line.startColor = line.endColor = color;
        line.widthMultiplier = width;
        int count = line.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = start + sweep * i / (count - 1);
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * flatten, 0));
        }
    }
}
