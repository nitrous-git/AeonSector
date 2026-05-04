using UnityEngine;

public class UnitVisualAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;

    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private CombatUnit ownerUnit;

    private float baseScaleX = 1f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (visualRoot == null)
            visualRoot = transform;

        if (ownerUnit == null)
            ownerUnit = GetComponentInParent<CombatUnit>();

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

        animator.Play(IsBackFacing(facing) ? "Attack_BackNE" : "Attack_FrontSW");
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
        playerInputController.AnimEvent_FireRangedProjectile(ownerUnit);
    }

    public void AnimEvent_RangedAttackFinished()
    {
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