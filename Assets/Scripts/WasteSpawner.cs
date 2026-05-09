using System.Collections;
using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [Header("Waste Settings")]
    public GameObject[] wastePrefabs;
    public int amountToSpawn = 30;
    
    [Header("Scattering Settings")]
    public Vector3 spawnAreaSize = new Vector3(50, 0, 50);
    public float spawnHeight = 10f;
    public float delayBetweenSpawns = 0.05f;

    void Start()
    {
        // StartCoroutine is the safe way to handle sequences in Unity
        StartCoroutine(ScatterWasteRoutine());
    }

    IEnumerator ScatterWasteRoutine()
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            // 1. Pick a random prefab
            GameObject prefabToSpawn = wastePrefabs[Random.Range(0, wastePrefabs.Length)];

            // 2. Calculate a random position within the defined area
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                spawnHeight,
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            // 3. Instantiate on the Main Thread
            // We store the name in a variable first just to be safe, 
            // though inside a Coroutine this isn't strictly necessary.
            string spawnedName = prefabToSpawn.name;
            GameObject spawnedItem = Instantiate(prefabToSpawn, randomPos, Quaternion.identity);
            
            // Optional: Rename for a cleaner hierarchy
            spawnedItem.name = "Waste_" + spawnedName + "_" + i;

            // 4. Wait a tiny bit before spawning the next one
            // This prevents the "GetName" error and stops the game from freezing
            yield return new WaitForSeconds(delayBetweenSpawns);
        }

        Debug.Log("Waste scattering complete!");
    }

    // This helps you see the spawn area in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, spawnHeight, 0), spawnAreaSize);
    }
}