using UnityEngine;

// Presentation data only. Attack permissions, damage and ranges remain on UnitStats.
[CreateAssetMenu(menuName = "AeonSector/Combat VFX Profile")]
public sealed class CombatVfxProfile : ScriptableObject
{
    public Color energy = Color.cyan;
    public Color edge = new Color(0.03f, 0.14f, 0.22f);
    [Range(0.5f, 1.8f)] public float impactScale = 1f;
    [Range(0f, 0.3f)] public float impactShake = 0.06f;
    [Range(0.05f, 0.3f)] public float shakeDuration = 0.12f;
    [Range(0f, 2f)] public float meleeContact = 0.6f;
    [Range(0f, 2f)] public float meleeRecovery = 0.6f;
    public GameObject projectile;
}
