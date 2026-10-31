using UnityEngine;

public class UnitVisualAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;

    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private CombatUnit ownerUnit;

    [Header("Ranged presentation (negative timing keeps legacy defaults)")]
    [SerializeField] private float rangedReleaseDelay = -1f;
    [SerializeField] private float rangedAnimationDuration = -1f;
    [SerializeField] private Transform projectileOriginFrontSW;
    [SerializeField] private Transform projectileOriginBackNE;

    public float GetRangedReleaseDelay(float fallback)
        => rangedReleaseDelay < 0f ? fallback : Mathf.Clamp(rangedReleaseDelay, 0f, 3f);

    public float RangedAnimationDuration => Mathf.Clamp(rangedAnimationDuration, 0f, 4f);

    public Vector3 GetProjectileOrigin(UnitFacing facing, Vector3 fallback)
    {
        Transform origin = IsBackFacing(facing) ? projectileOriginBackNE : projectileOriginFrontSW;
        return origin != null ? origin.position : fallback;
    }

    private float baseScaleX = 1f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (visualRoot == null)
            visualRoot = transform;

        if (ownerUnit == null)
            ownerUnit = GetComponentInParent<CombatUnit>();

        if (playerInputController == null)
            playerInputController = FindFirstObjectByType<PlayerInputController>();

        baseScaleX = Mathf.Abs(visualRoot.localScale.x);
    }

    public void PlayIdle(UnitFacing facing)
    {
        ApplyFacing(facing);

        if (animator == null)
            return;

        animator.Play(IsBackFacing(facing) ? "Idle_BackNE" : "Idle_FrontSW");
    }

    public void PlayAttack(UnitFacing facing)
    {
        Debug.Log("Attack Facing : " + facing);
        ApplyFacing(facing);

        if (animator == null)
            return;

        string attackState = IsBackFacing(facing) ? "Attack_BackNE" : "Attack_FrontSW";
        if (animator.HasState(0, Animator.StringToHash(attackState))) animator.Play(attackState);
        else PlayIdle(facing); // Static artillery presentation has idle clips only.
    }

    public void PlayAttack(UnitFacing facing, CommandMode commandMode)
    {
        Debug.Log("Attack Facing : " + facing + " with commandMode overload :" + commandMode.ToString());
        ApplyFacing(facing);

        if (animator == null)
            return;

        switch (commandMode)
        { 
            case CommandMode.MeleeAttack:
                animator.Play(IsBackFacing(facing) ? "MeleeAttack_BackNE" : "MeleeAttack_FrontSW");
                break;
            case CommandMode.RangedAttack:
                animator.Play(IsBackFacing(facing) ? "RangedAttack_BackNE" : "RangedAttack_FrontSW");
                break;
        }
    }

    // Ranged attack animation triggered event 

    public void AnimEvent_FireRangedProjectile()
    {
        if (playerInputController != null && ownerUnit != null
            && ownerUnit.OwnerFaction != null && ownerUnit.OwnerFaction.Type == FactionType.Player)
            playerInputController.AnimEvent_FireRangedProjectile(ownerUnit);
    }

    public void AnimEvent_RangedAttackFinished()
    {
        if (playerInputController != null && ownerUnit != null
            && ownerUnit.OwnerFaction != null && ownerUnit.OwnerFaction.Type == FactionType.Player)
            playerInputController.AnimEvent_RangedAttackFinished(ownerUnit);
    }

    // Helpers 

    private void ApplyFacing(UnitFacing facing)
    {
        if (visualRoot == null)
            return;

        float sign = IsMirrored(facing) ? -1f : 1f;

        Vector3 scale = visualRoot.localScale;
        scale.x = baseScaleX * sign;
        visualRoot.localScale = scale;
    }

    private bool IsBackFacing(UnitFacing facing)
    {
        return facing == UnitFacing.NorthEast || facing == UnitFacing.NorthWest;
    }

    private bool IsMirrored(UnitFacing facing)
    {
        return facing == UnitFacing.SouthEast || facing == UnitFacing.NorthWest;
    }
}
