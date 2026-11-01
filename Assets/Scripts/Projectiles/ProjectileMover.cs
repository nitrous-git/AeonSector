using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ProjectileMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private bool rotateTowardTarget = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float hitAnimationDuration = 0.40f;
    [SerializeField] private bool hideOnContact;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public IEnumerator FlyAndHit(Vector3 startWorldPosition, Vector3 targetWorldPosition, System.Action onContact = null)
    {
        transform.position = startWorldPosition;

        Vector3 direction = targetWorldPosition - startWorldPosition;
        direction.z = 0f;

        if (rotateTowardTarget && direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (animator != null)
        {
            animator.Play("Missile_Fly");
        }

        while ((transform.position - targetWorldPosition).sqrMagnitude > 0.0025f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, Mathf.Max(0.1f, moveSpeed) * Time.deltaTime);
            yield return null;
        }

        transform.position = targetWorldPosition;

        // Resolve once on arrival, not after the visual recovery. Callers retain their guards/locks.
        onContact?.Invoke();
        if (hideOnContact)
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.enabled = false;

        if (animator != null)
        {
            animator.Play("Missile_Hit");
        }

        if (hitAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(hitAnimationDuration);
        }

        Destroy(gameObject);
    }

}
