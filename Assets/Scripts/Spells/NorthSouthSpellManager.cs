using UnityEngine;
using System.Collections.Generic;

public class NorthSouthSpellManager : MonoBehaviour
{
    [Header("North/South Attraction Settings")]
    [Tooltip("Strength of the gravitational pull from South objects towards the North object.")]
    public float attractionStrength = 15f;

    // Singleton tracker for the active North object in the scene[cite: 2]
    private static GameObject currentNorthObj;
    public static Transform CurrentNorthTransform { get; private set; }

    // Track all active South rigidbodies for attraction calculations[cite: 2]
    private static List<Rigidbody> southRigidbodies = new List<Rigidbody>();

    public void ApplyNorth(Collider targetCollider)
    {
        if (targetCollider == null) return;

        GameObject targetObj = targetCollider.gameObject;
        Animator animator = targetObj.GetComponentInChildren<Animator>();
        Rigidbody rb = targetObj.GetComponent<Rigidbody>();

        // 1. If this object was previously registered as South, remove it from the South tracking list
        if (rb != null && southRigidbodies.Contains(rb))
        {
            southRigidbodies.Remove(rb);
        }

        // 2. If there is already a *different* North object, remove its North status and reset its animator[cite: 2]
        if (currentNorthObj != null && currentNorthObj != targetObj)
        {
            Animator oldAnimator = currentNorthObj.GetComponentInChildren<Animator>();
            if (oldAnimator != null)
            {
                oldAnimator.SetBool("isNorth", false);
            }
            Debug.Log($"Removed North status from previous object: {currentNorthObj.name}[cite: 2]");
        }

        // 3. Assign the new North object[cite: 2]
        currentNorthObj = targetObj;
        CurrentNorthTransform = currentNorthObj.transform;

        // 4. Update Animator parameters: ensure isSouth is turned OFF and isNorth is turned ON
        if (animator != null)
        {
            animator.SetBool("isSouth", false);
            animator.SetBool("isNorth", true); // Trigger the "isNorth" animator parameter[cite: 2]
        }

        Debug.Log($"Object **{currentNorthObj.name}** is now marked as **NORTH**[cite: 2].");
    }

    public void ApplySouth(Collider targetCollider)
    {
        if (targetCollider == null) return;

        GameObject targetObj = targetCollider.gameObject;
        Animator animator = targetObj.GetComponentInChildren<Animator>();
        Rigidbody rb = targetObj.GetComponent<Rigidbody>();

        // 1. If this object was currently the North singleton, strip its North status first
        if (currentNorthObj == targetObj)
        {
            currentNorthObj = null;
            CurrentNorthTransform = null;
            Debug.Log($"Object {targetObj.name} is no longer North; converting to South.");
        }

        // 2. Add to South rigidbodies list if not already tracked[cite: 2]
        if (rb != null && !southRigidbodies.Contains(rb))
        {
            southRigidbodies.Add(rb);
        }

        // 3. Update Animator parameters: ensure isNorth is turned OFF and isSouth is turned ON
        if (animator != null)
        {
            animator.SetBool("isNorth", false);
            animator.SetBool("isSouth", true); // Trigger the "isSouth" animator parameter[cite: 2]
        }

        Debug.Log($"Object **{targetCollider.name}** is now marked as **SOUTH**[cite: 2].");
    }

    void FixedUpdate()
    {
        // If a North object exists, pull all registered South rigidbodies towards it[cite: 2]
        if (CurrentNorthTransform != null)
        {
            for (int i = southRigidbodies.Count - 1; i >= 0; i--)
            {
                Rigidbody southRb = southRigidbodies[i];
                
                // Clean up destroyed objects from the list automatically[cite: 2]
                if (southRb == null)
                {
                    southRigidbodies.RemoveAt(i);
                    continue;
                }

                Vector3 direction = CurrentNorthTransform.position - southRb.position;
                float distance = direction.magnitude;

                if (distance > 0.2f)
                {
                    Vector3 pullForce = direction.normalized * attractionStrength;
                    southRb.AddForce(pullForce, ForceMode.Acceleration); // Apply gravitational pull[cite: 2]
                }
            }
        }
    }
}