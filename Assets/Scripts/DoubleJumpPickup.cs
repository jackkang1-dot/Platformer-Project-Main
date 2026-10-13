using UnityEngine;

/// <summary>
/// A collectible that unlocks the double jump when the player touches it.
/// Needs a Collider2D on the same object with Is Trigger ticked.
/// If the player dies afterwards, the level restarts and the pickup is back.
/// Optionally shows an on-screen message (Unlock Notification) when collected.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoubleJumpPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Optional: drag in the Canvas that has the Unlock Notification script, so its message pops up when this is collected.")]
    [SerializeField] private UnlockNotification notification;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Use the object that owns the Rigidbody, in case the touching collider sits on a child
        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!target.CompareTag(playerTag)) return;

        PlayerDoubleJump doubleJump = target.GetComponent<PlayerDoubleJump>();
        if (doubleJump == null) return;

        doubleJump.Unlocked = true;
        Debug.Log("Double jump unlocked!");

        if (notification != null) notification.Show();

        Destroy(gameObject);
    }
}
