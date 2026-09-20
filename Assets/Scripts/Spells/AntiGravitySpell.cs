using UnityEngine;

public class AntiGravitySpell : MonoBehaviour
{
    [Header("Anti-Gravity Settings")]
    public float levitationForce = 15f;
    public float duration = 5f;

    public void Apply(Rigidbody targetRb)
    {
        if (targetRb == null) return;

        // Check if it already has the component, if not, add it
        AntiGravityEffect effect = targetRb.GetComponent<AntiGravityEffect>();
        if (effect == null)
        {
            effect = targetRb.gameObject.AddComponent<AntiGravityEffect>();
        }
        effect.Initialize(levitationForce, duration);
    }
}

// Helper runtime component attached dynamically to the target object
public class AntiGravityEffect : MonoBehaviour
{
    private Rigidbody rb;
    private float force;
    private float timer;
    private bool originalUseGravity;

    public void Initialize(float levitationForce, float effectDuration)
    {
        rb = GetComponent<Rigidbody>();
        force = levitationForce;
        timer = effectDuration;

        if (rb != null)
        {
            originalUseGravity = rb.useGravity;
            rb.useGravity = false; // Turn off standard gravity
            rb.linearVelocity = Vector3.zero; // Reset momentum
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            // Apply upward force to simulate reverse gravity / levitation
            rb.AddForce(Vector3.up * force, ForceMode.Acceleration);

            timer -= Time.fixedDeltaTime;
            if (timer <= 0f)
            {
                // Revert back to original state
                rb.useGravity = originalUseGravity;
                Destroy(this); // Remove component when effect ends
            }
        }
    }
}