using UnityEngine;

public class SwapSpell : MonoBehaviour
{
    [Header("Swap Safety Settings")]
    [Tooltip("Layers considered walls or obstacles.")]
    public LayerMask obstacleMask;
    [Tooltip("Radius used to check if a destination position is clear of walls.")]
    public float safetyRadius = 1f;

    public void ExecuteSwap(Transform playerTransform, CharacterController playerController, Rigidbody targetRb)
    {
        if (targetRb == null || playerTransform == null) return;

        Vector3 originalPlayerPos = playerTransform.position;
        Vector3 originalTargetPos = targetRb.position;

        // Calculate safe positions to avoid spawning inside walls due to size/position differences
        Vector3 safePlayerPos = GetSafePosition(originalTargetPos, safetyRadius, obstacleMask);
        Vector3 safeTargetPos = GetSafePosition(originalPlayerPos, safetyRadius, obstacleMask);

        // Temporarily disable CharacterController so Unity doesn't block the teleport
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Perform the position swap
        playerTransform.position = safePlayerPos;
        targetRb.position = safeTargetPos;

        // Reset target object velocities so it doesn't fly off unpredictably
        targetRb.linearVelocity = Vector3.zero;
        targetRb.angularVelocity = Vector3.zero;

        // Re-enable CharacterController
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log("Swapped positions between player and target safely!");
    }

    private Vector3 GetSafePosition(Vector3 desiredPos, float radius, LayerMask mask)
    {
        // Check if the desired destination overlaps with any walls/obstacles
        if (!Physics.CheckSphere(desiredPos, radius, mask))
        {
            return desiredPos; // Position is completely clear
        }

        // If blocked, try shifting upwards to find clearance (useful if objects are near floors/walls)
        Vector3 adjustedPos = desiredPos + Vector3.up * (radius * 1.5f);
        if (!Physics.CheckSphere(adjustedPos, radius, mask))
        {
            return adjustedPos;
        }

        // Fallback to original desired position if adjustments fail
        return desiredPos;
    }
}