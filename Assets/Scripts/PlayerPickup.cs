using UnityEngine;
using TMPro;

public class PlayerPickup : MonoBehaviour
{
    private bool isOverBin = false;
    
    [Header("UI References")]
    public TextMeshProUGUI messageText; 

    [Header("Holding Settings")]
    public Transform holdPoint;

    private GameObject nearbyWaste;
    private GameObject heldWaste;

    void Start()
    {
        // Hide the message at the very start
        UpdateUI("");
    }

    void Update()
    {
        // NEW: If the game is over (Time.timeScale is 0), don't allow any picking or dropping
        if (Time.timeScale == 0) return;

        // 1. Check for Pickup
        if (Input.GetKeyDown(KeyCode.E) && nearbyWaste != null && heldWaste == null)
        {
            PlayerPickUpAction();
        }

        // 2. Check for Drop
        if (Input.GetKeyDown(KeyCode.Q) && heldWaste != null)
        {
            DropWaste();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.timeScale == 0) return;

        // Detecting Waste to pick up
        if (other.CompareTag("Degradable") || other.CompareTag("NonDegradable"))
        {
            nearbyWaste = other.gameObject;
            // Only show pick up message if we aren't already holding something
            if (heldWaste == null) UpdateUI("Press E to pick up");
        }

        // Detecting the Bin to drop
        if (other.GetComponent<WasteBin>() != null)
        {
            isOverBin = true;
            if (heldWaste != null) UpdateUI("Press Q to drop in Bin");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == nearbyWaste) 
        { 
            nearbyWaste = null; 
            UpdateUI(""); 
        }
        
        if (other.GetComponent<WasteBin>() != null)
        {
            isOverBin = false;
            // If we are still holding waste, change message back to general drop
            if (heldWaste != null) UpdateUI("Press Q to Drop");
            else UpdateUI("");
        }
    }

    void PlayerPickUpAction()
    {
        heldWaste = nearbyWaste;

        Rigidbody rb = heldWaste.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = heldWaste.GetComponent<Collider>();
        if (col != null) col.isTrigger = true; 

        heldWaste.transform.SetParent(holdPoint);
        heldWaste.transform.localPosition = Vector3.zero;
        heldWaste.transform.localRotation = Quaternion.identity;

        UpdateUI("Press Q to Drop");
        nearbyWaste = null;
    }

    void DropWaste()
    {
        Collider col = heldWaste.GetComponent<Collider>();
        // Note: Keep isTrigger = true here if you want it to fall INTO the bin trigger 
        // without bouncing off the rim.
        col.isTrigger = true; 

        heldWaste.transform.SetParent(null);
        Rigidbody rb = heldWaste.GetComponent<Rigidbody>();

        if (rb != null) 
        {
            rb.isKinematic = false;
            
            if (isOverBin) 
            {
                rb.linearVelocity = Vector3.zero; 
            }
            else 
            {
                rb.AddForce(transform.forward * 2f, ForceMode.Impulse);
                // If dropping on street, turn physics collision back on
                col.isTrigger = false; 
            }
        }

        heldWaste = null;
        UpdateUI(""); // Hide UI immediately after dropping
    }

    private void UpdateUI(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;

            // NEW: If the message is empty, turn off the GameObject so it's invisible
            // If it's not empty, turn it on.
            if (string.IsNullOrEmpty(msg))
            {
                messageText.gameObject.SetActive(false);
            }
            else
            {
                messageText.gameObject.SetActive(true);
            }
        }
    }
}