using UnityEngine;

// Small helper component to mark an object as having been cloned already
public class ClonedObjectTracker : MonoBehaviour {}

public class CloneSpell : MonoBehaviour
{
    [Header("Clone Settings")]
    [Tooltip("Height offset above the target object where the clone spawns.")]
    public float spawnHeightOffset = 2.0f;

    public void ExecuteClone(Collider targetCollider)
    {
        if (targetCollider == null) return;

        Transform targetTransform = targetCollider.transform;

        // 1. Mark the original object so it can't be cloned again
        if (targetCollider.GetComponent<ClonedObjectTracker>() == null)
        {
            targetCollider.gameObject.AddComponent<ClonedObjectTracker>();
        }

        // Calculate spawn position directly above the target object
        Vector3 spawnPosition = targetTransform.position + (Vector3.up * spawnHeightOffset);

        // Instantiate a copy of the target object
        GameObject cloneObj = Instantiate(targetTransform.gameObject, spawnPosition, targetTransform.rotation);

        // 2. Mark the clone with the "Clone" tag and also give it the tracker component
        try
        {
            cloneObj.tag = "Clone";
        }
        catch (System.Exception)
        {
            Debug.LogWarning("The 'Clone' tag is not defined in your Tag Manager! Please add it in Unity under Tags & Layers.");
        }

        if (cloneObj.GetComponent<ClonedObjectTracker>() == null)
        {
            cloneObj.gameObject.AddComponent<ClonedObjectTracker>();
        }

        // Clean up Rigidbody physics on the clone so it doesn't inherit weird momentum
        Rigidbody cloneRb = cloneObj.GetComponent<Rigidbody>();
        if (cloneRb != null)
        {
            cloneRb.linearVelocity = Vector3.zero;
            cloneRb.angularVelocity = Vector3.zero;
        }

        // Set the "isClone" boolean on any Animator found on the clone
        Animator cloneAnimator = cloneObj.GetComponentInChildren<Animator>();
        if (cloneAnimator != null)
        {
            cloneAnimator.SetBool("isClone", true);
        }

        Debug.Log($"Successfully created a clone of **{targetTransform.name}**! Neither the original nor the clone can be cloned again.");
    }
}