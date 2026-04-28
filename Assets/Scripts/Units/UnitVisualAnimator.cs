using UnityEngine;

public class UnitVisualAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;

    private float baseScaleX = 1f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (visualRoot == null)
            visualRoot = transform;

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