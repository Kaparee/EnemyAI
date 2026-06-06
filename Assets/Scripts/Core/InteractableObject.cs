using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableObject : MonoBehaviour {
    public AreaSpawnerManager manager;
    public GameObject parentArea;
    public List<ResourceStack> lootTable = new List<ResourceStack>();

    public AsteroidSavedData myData;
    public BeltSavedData myBelt;

    public float distanceBetweenObjects;

    private float timer;

    [Header("Asteroid Explosion")]
    [SerializeField] private GameObject explosionPrefab;

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {

            if (manager != null) {
                manager.OnObjectInteracted(parentArea, myBelt);
            }

            Debug.Log("Kolizja z asteroidą! Ostrożnie z manewrami.");
        }
    }

    private void Update()
    {
        if (parentArea == null) return;

        timer += Time.deltaTime;
        if (timer >= 2f)
        {
            distanceBetweenObjects = Vector3.Distance(transform.position, parentArea.transform.position);
            Debug.DrawLine(transform.position, parentArea.transform.position, Color.green);
            CheckDistance();
            timer = 0f;
        }
    }

    void CheckDistance()
    {
        if (distanceBetweenObjects > 250)
        {

            if (manager != null)
            {
                manager.OnObjectInteracted(parentArea, myBelt);
            }

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(
                    explosionPrefab,
                    transform.position,
                    transform.rotation
                );

                Scene asteroidScene = gameObject.scene;
                SceneManager.MoveGameObjectToScene(explosion, asteroidScene);
            }

            Destroy(gameObject);
            Debug.Log("Asteroida zniszczona poza obszarem sektora.");
        }
    }
}