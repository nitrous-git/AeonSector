using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CombatUnit))]
public class UnitMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.1f;
    [SerializeField] private Animator animator;

    private CombatUnit unit;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        unit = GetComponent<CombatUnit>();
    }

    public IEnumerator MoveAlongPath(TilemapBoardAdapter board, List<GridCoord> path)
    {
        if (IsMoving || board == null || path == null || path.Count <= 1)
        {
            yield break; 
        }

        IsMoving = true;

        for (int i = 1; i < path.Count; i++)
        {
            GridCoord fromCoord = path[i - 1];
            GridCoord nextCoord = path[i];

            unit.FaceFromTo(fromCoord, nextCoord);
            unit.PlayIdle();

            Vector3 targetWorldPos = board.ConvertGridToWorld(nextCoord);

            while ((transform.position - targetWorldPos).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                yield return null;  
            }

            transform.position = targetWorldPos;
        }

        unit.PlayIdle();
        IsMoving = false;
    }
}
