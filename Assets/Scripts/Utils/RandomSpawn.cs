using UnityEngine;

public class RandomSpawn : MonoBehaviour
{
    [SerializeField] private GameObject[] itemPrefab;

    void Start()
    {
        SpawnRandomItem();
    }

public void SpawnRandomItem()
    {
        if (itemPrefab == null || itemPrefab.Length == 0)
        {
            Debug.LogWarning("Missing Item Prefab");
            return;
        }

        GameObject itemToSpawn = itemPrefab[Random.Range(0, itemPrefab.Length)];
        GameObject spawnedPickup = Instantiate(itemToSpawn, transform.position, Quaternion.identity);
    }
}
