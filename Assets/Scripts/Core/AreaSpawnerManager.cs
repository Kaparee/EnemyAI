using UnityEngine;
using System.Collections.Generic;

public class AreaSpawnerManager : MonoBehaviour
{
    [Header("Resources")]
    public ResourceDatabase database;
    
    [Header("Area settings")]
    public Vector3 areaSize = new Vector3(300f, 40f, 80f);
    public Vector3 worldSpawnSize = new Vector3(500f, 0f, 500f);

    [Header("Objects")]
    public GameObject[] prefabs;
    private GameObject player;
    private List<GameObject> areas = new List<GameObject>();

    public int currentSectorStage;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void InitialSpawn(SectorData data) {
        if (!data.hasAsteroidGroup) return;

        foreach (BeltSavedData belt in data.belts) {

            GameObject areaObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            areaObj.transform.position = transform.position + belt.beltCenter;
            areaObj.transform.localScale = areaSize;
            areaObj.transform.SetParent(this.transform);
            SetupMaterial(areaObj);

            foreach (AsteroidSavedData astData in belt.asteroids) {

                Vector3 worldPos = (transform.position + belt.beltCenter) + astData.localPos;
                GameObject obj = Instantiate(prefabs[Random.Range(0, prefabs.Length)], worldPos, Quaternion.identity, this.transform);

                InteractableObject io = obj.GetComponent<InteractableObject>();
                if (io == null) {
                    io = obj.AddComponent<InteractableObject>();
                }

                io.manager = this;
                io.parentArea = areaObj;
                io.myBelt = belt;
                io.myData = astData;

                Asteroid asteroidVisual = obj.GetComponent<Asteroid>();
            }
            areas.Add(areaObj);
        }
    }

    public void OnObjectInteracted(GameObject currentArea, BeltSavedData beltData)
    {
        int emptyCount = 0;
        int totalAsteroids = beltData.asteroids.Count;


        float minedPercentage = (float)emptyCount / totalAsteroids;

        Debug.Log($"Belt: {beltData.beltCenter} | Mined: {emptyCount}/{totalAsteroids} ({minedPercentage * 100}%)");
        if (minedPercentage >= 0.80f && !beltData.respawnTriggered) {
            if (ChunkManager.Instance != null) {
                ChunkManager.Instance.TrySpawnNewBeltGlobal();
            }
            beltData.respawnTriggered = true;
        }

        if (emptyCount == totalAsteroids) {
            if (areas.Contains(currentArea)) areas.Remove(currentArea);
            if (currentArea != null) Destroy(currentArea);
        }
    }

    public void SetupMaterial(GameObject area) {
        area.layer = 2;

        BoxCollider col = area.GetComponent<BoxCollider>();
        if (col != null) {
            col.isTrigger = true;
            col.enabled = true;
        }

        MeshRenderer mr = area.GetComponent<MeshRenderer>();
        if (mr != null) {
            mr.enabled = false;
        }

        MeshFilter mf = area.GetComponent<MeshFilter>();
        if (mf != null) {
            Destroy(mf);
        }
    }
}
