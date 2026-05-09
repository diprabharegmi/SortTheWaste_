using UnityEngine; // This line is required to fix the 'Collider' error

public class WasteBin : MonoBehaviour
{
    [Header("Bin Settings")]
    public WasteItem.WasteType acceptedType;
    
    [Header("References")]
    public GameSystemManager gameManager; // Drag your Game Manager object here in the Inspector

    private void OnTriggerEnter(Collider other)
{
    WasteItem item = other.GetComponent<WasteItem>();

    if (item != null)
    {
        // FIX: If the item still has a parent, it means the player is holding it.
        // We ignore it until the player drops it!
        if (other.transform.parent != null) 
        {
            return; 
        }

        if (item.type == acceptedType)
        {
            if (gameManager != null) gameManager.CorrectSort();
            Destroy(other.gameObject); 
        }
        else
        {
            if (gameManager != null) gameManager.WrongSort();
            Destroy(other.gameObject); 
        }
    }
}
}