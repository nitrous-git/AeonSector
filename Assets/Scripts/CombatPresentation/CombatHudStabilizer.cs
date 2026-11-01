using Unity.Cinemachine;
using UnityEngine;

// The showcase HUD is world-space. Cancel only Cinemachine's positional correction,
// preserving its existing layout/animations and leaving unit health bars in the world.
[DefaultExecutionOrder(10000)]
public sealed class CombatHudStabilizer : MonoBehaviour
{
    public CinemachineBrain brain;
    public Transform[] hudRoots;
    private Vector3 appliedOffset;

    private void Update() => RemoveOffset();

    private void LateUpdate()
    {
        if (brain == null || hudRoots == null) return;
        appliedOffset = brain.State.PositionCorrection;
        appliedOffset.z = 0;
        foreach (var root in hudRoots)
            if (root != null) root.position += appliedOffset;
    }

    private void RemoveOffset()
    {
        if (hudRoots != null)
            foreach (var root in hudRoots)
                if (root != null) root.position -= appliedOffset;
        appliedOffset = Vector3.zero;
    }

    private void OnDisable() => RemoveOffset();
}
