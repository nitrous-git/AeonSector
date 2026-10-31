using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatUnit : MonoBehaviour
{
    [SerializeField] private UnitStats stats;
    [SerializeField] private UnitFacing startingFacing = UnitFacing.SouthWest;
    [SerializeField] private UnitVisualAnimator visualAnimator;
   
    public UnitStats Stats => stats;
    public Faction OwnerFaction { get; private set; }
    public GridCoord GridPosition { get; private set; }

    public UnitFacing Facing { get; private set; }

    public float GetRangedReleaseDelay(float fallback)
        => visualAnimator != null ? visualAnimator.GetRangedReleaseDelay(fallback) : fallback;

    public float RangedAnimationDuration => visualAnimator != null ? visualAnimator.RangedAnimationDuration : 0f;

    public Vector3 GetProjectileOrigin(Vector3 fallback)
        => visualAnimator != null ? visualAnimator.GetProjectileOrigin(Facing, fallback) : fallback;

    public int CurrentHP { get; private set; }
    public bool HasMoved { get; private set; }
    public bool HasAttacked { get; private set; }

    public bool IsAlive => CurrentHP > 0;
    public bool CanMove => IsAlive && !HasMoved;
    public bool CanAttack => IsAlive && !HasAttacked;
    public bool IsTurnExhausted => !CanMove && !CanAttack;

    [SerializeField] private Slider healthBar;
    private bool deathProcessed;

    private void Awake()
    {
        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<UnitVisualAnimator>();
        }

        if (healthBar == null)
            healthBar = GetComponentInChildren<Slider>(true);
        if (healthBar == null)
            Debug.LogWarning($"CombatUnit '{name}' has no health Slider assigned or in its children.", this);

    }

    public void Initialize(Faction ownerFaction, GridCoord startCoord)
    {
        OwnerFaction = ownerFaction;
        GridPosition = startCoord;

        //Debug.Log($"{ownerFaction.Type.ToString()} , {startCoord}");

        if (stats == null) {
            Debug.LogError($"CombatUnit '{name}' has no UnitStats assigned");
            return;
        }

        CurrentHP = stats.MaxHP;
        if (healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = stats.MaxHP;
            healthBar.value = CurrentHP;
        }
        HasMoved = false;
        HasAttacked = false;
        deathProcessed = false;

        SetFacing(startingFacing);
        PlayIdle();
    }

    public void BeginTurn()
    {
        if (!IsAlive) { return; }
        HasMoved = false;
        HasAttacked = false;
    }

    public void MarkMoved()
    {
        if (!IsAlive) { return; }
        HasMoved = true;
    }

    public void MarkAttacked()
    {
        if (!IsAlive) { return; }
        HasAttacked = true;
    }

    public void SetGridPosition(GridCoord coord)
    {
        GridPosition = coord;
    }

    public bool TakeDamage(int amount)
    {
        if (!IsAlive) 
        { 
            return false; 
        }

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        if (healthBar != null)
            healthBar.value = CurrentHP;

        if (CurrentHP == 0)
        {
            OnDeath();
            return true;
        }

        if (OwnerFaction == null)
            return false;

        switch (OwnerFaction.Type)
        {
            case FactionType.Player: SFXManager.PlayerHit(); break;
            case FactionType.Enemy: SFXManager.EnemyHit(); break;
        }

        return false;
    }

    private void OnDeath()
    {
        if (deathProcessed)
            return;

        deathProcessed = true;
        HasMoved = true;
        HasAttacked = true;
        Debug.Log($"{name} has been destroyed.");
    }

    // -----------------------
    // Facing helpers 
    // -----------------------

    public void SetFacing(UnitFacing facing)
    {
        Facing = facing;
    }

    public void FaceTowards(GridCoord targetCoord)
    {
        Facing = GetFacingFromDelta(GridPosition, targetCoord);
    }

    public void FaceFromTo(GridCoord fromCoord, GridCoord toCoord)
    {
        Facing = GetFacingFromDelta(fromCoord, toCoord);
    }

    public void PlayIdle()
    {
        if (visualAnimator != null)
            visualAnimator.PlayIdle(Facing);
    }

    public void PlayAttack()
    {
        if (visualAnimator != null)
            visualAnimator.PlayAttack(Facing);
    }

    public void PlayAttack(CommandMode commandMode)
    {
        if (visualAnimator != null)
            visualAnimator.PlayAttack(Facing, commandMode);
    }

    private UnitFacing GetFacingFromDelta(GridCoord from, GridCoord to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        Debug.Log($"dx : {dx}, dy = {dy}");

        if (dx == 0 && dy == 0)
            return Facing;

        // Fix the mapping here
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            Debug.Log($"dx > dy");
            return dx < 0 ? UnitFacing.SouthWest : UnitFacing.NorthEast;
        }

        Debug.Log($"dx < dy");
        return dy < 0 ? UnitFacing.SouthEast : UnitFacing.NorthWest;
    }

    // -----------------------
    // Energy Calculation
    // -----------------------

    public float GetEnergyPercent()
    {
        float hpPercent = stats.MaxHP > 0 ? Mathf.Clamp01((float)CurrentHP / stats.MaxHP) : 0f;

        float energy = 0f;

        // 70% comes from HP.
        energy += hpPercent * 0.70f;

        // 15% if the unit can still move.
        if (CanMove)
        {
            energy += 0.15f;
        }

        // 15% if the unit can still attack.
        if (CanAttack)
        {
            energy += 0.15f;
        }

        return Mathf.Clamp01(energy);
    }

    public float GetEnergyUsedPercent()
    {
        return 1f - GetEnergyPercent();
    }

    // -----------------------
    // Pretty printing
    // -----------------------

    public override string ToString()
    {
        string factionName = OwnerFaction != null ? OwnerFaction.Type.ToString() : "None";
        string unitName = stats != null ? stats.UnitName : "NoStats";

        return $"{unitName} [{factionName}] HP:{CurrentHP} Pos:{GridPosition} Facing:{Facing} Moved:{HasMoved} Attacked:{HasAttacked}";
    }

}
