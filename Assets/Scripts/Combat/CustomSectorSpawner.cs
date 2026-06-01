using UnityEngine;

public class CustomSectorSpawner : MonoBehaviour
{
    public float sectorRadius = 100f;
    public AIArchetype[] availableArchetypes;

    public float spawnInterval = 10f;
    private float nextSpawnTime;

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            ManualSpawnOnEdge();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void ManualSpawnOnEdge()
    {
        if (availableArchetypes == null || availableArchetypes.Length == 0) return;

        float randomAngleRads = Random.value * Mathf.PI * 2f;
        float edgeX = Mathf.Cos(randomAngleRads) * sectorRadius;
        float edgeZ = Mathf.Sin(randomAngleRads) * sectorRadius;

        Vector3 spawnPosition = new Vector3(
            transform.position.x + edgeX,
            transform.position.y,
            transform.position.z + edgeZ
        );

        AIArchetype chosen = availableArchetypes[Random.Range(0, availableArchetypes.Length)];

        GameObject newEnemy = Instantiate(chosen.prefab, spawnPosition, Quaternion.identity);

        float dx = transform.position.x - spawnPosition.x;
        float dz = transform.position.z - spawnPosition.z;
        float lookAngle = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        newEnemy.transform.eulerAngles = new Vector3(0, lookAngle, 0);

        Debug.Log($"Wróg zespawnowany ręcznie. Kąt: {randomAngleRads} rad.");
    }
}