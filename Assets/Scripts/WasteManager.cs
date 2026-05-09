using UnityEngine;
using TMPro;

public class WasteManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI messageText; 

    [Header("Holding Settings")]
    public Transform holdPoint;

    private GameObject nearbyWaste;
    private GameObject heldWaste;

    void Update()
    {
        // Check for Pickup
        if (Input.GetKeyDown(KeyCode.E) && nearbyWaste != null && heldWaste == null)
        {
            PlayerPickUp();
        }

        // Check for Drop
        if (Input.GetKeyDown(KeyCode.Q) && heldWaste != null)
        {
            DropWaste();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Degradable") || other.CompareTag("NonDegradable"))
        {
            nearbyWaste = other.gameObject;
            UpdateUI("Press E to pick up waste");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == nearbyWaste)
        {
            nearbyWaste = null;
            UpdateUI("");
        }
    }

    // --- MAIN FUNCTIONS ---

    void PlayerPickUp()
    {
        heldWaste = nearbyWaste;

        // Disable physics
        Rigidbody rb = heldWaste.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Parent to hand and reset position
        heldWaste.transform.SetParent(holdPoint);
        heldWaste.transform.localPosition = Vector3.zero;
        heldWaste.transform.localRotation = Quaternion.identity;

        UpdateUI("Waste Picked Up (Press Q to Drop)");
        nearbyWaste = null;
    }

    // This is now outside of any other functions, solving the warning
    void DropWaste()
    {
        // Unparent
        heldWaste.transform.SetParent(null);

        // Re-enable physics
        Rigidbody rb = heldWaste.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = false;
            // Optional: Give it a little nudge forward so it doesn't drop on your toes
            rb.AddForce(transform.forward * 2f, ForceMode.Impulse);
        }

        heldWaste = null;
        UpdateUI("Waste Dropped");
    }

    private void UpdateUI(string msg)
    {
        if (messageText != null) messageText.text = msg;
    }
}