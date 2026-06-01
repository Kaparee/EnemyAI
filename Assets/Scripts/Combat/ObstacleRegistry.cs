using UnityEngine;
using System.Collections.Generic;

public class ObstacleRegistry : MonoBehaviour
{
    private static ObstacleRegistry _instance;
    public static ObstacleRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                ObstacleRegistry existing = FindFirstObjectByType<ObstacleRegistry>();
                if (existing != null)
                {
                    _instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("ObstacleRegistry");
                    _instance = go.AddComponent<ObstacleRegistry>();
                }
            }
            return _instance;
        }
    }

    public List<Vector4> Obstacles = new List<Vector4>();

    private float updateTimer = 0f;
    private float updateInterval = 1.5f;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        RefreshObstacles();
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            RefreshObstacles();
            updateTimer = 0f;
        }
    }

    private void RefreshObstacles()
    {
        Obstacles.Clear();
        int asteroidLayer = AILayers.Asteroid;

        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (Collider col in allColliders)
        {
            if (col.isTrigger) continue;

            bool isObstacle = col.CompareTag("Asteroid")
                || (asteroidLayer >= 0 && col.gameObject.layer == asteroidLayer);

            if (!isObstacle) continue;

            float radius = col.bounds.extents.magnitude;
            Vector3 pos = col.bounds.center;
            Obstacles.Add(new Vector4(pos.x, pos.y, pos.z, radius));
        }
    }

    public bool IsPositionBlocked(Vector3 pos, float checkRadius)
    {
        for (int i = 0; i < Obstacles.Count; i++)
        {
            Vector4 obs = Obstacles[i];
            Vector3 obsPos = new Vector3(obs.x, obs.y, obs.z);
            if (Vector3.Distance(pos, obsPos) < obs.w + checkRadius)
                return true;
        }
        return false;
    }
}
