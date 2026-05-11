using UnityEngine;
using TMPro;

public class Sector : MonoBehaviour {
    private SectorData data;

    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject repairStationPrefab;
    public void Setup(SectorData newData, float size) {
        this.data = newData;
        //Debug.Log($"Sektor {data.gridPosition}");

        AreaSpawnerManager spawner = GetComponent<AreaSpawnerManager>();
        if (spawner != null) {
            spawner.currentSectorStage = data.sectorStage;
            spawner.InitialSpawn(data);
        }

        if (newData.haveShop == true)
        {
            GameObject shop = Instantiate(shopPrefab, transform);
            shop.transform.localPosition = data.shopLocalPos;
        }

        if (newData.haveRepairStation == true)
        {
            GameObject repairStation = Instantiate(repairStationPrefab, transform);
            repairStationPrefab.transform.localPosition = data.repairStationLocalPos;
        }

        // Rysujemy obwódki sektora
        //DrawSectorBorder(size);
    }

        //private void DrawSectorBorder(float size) {
        //    LineRenderer line = gameObject.GetComponent<LineRenderer>();
        //    if (line == null) {
        //        line = gameObject.AddComponent<LineRenderer>();
        //    }

        //    line.startWidth = 4f;
        //    line.endWidth = 4f;
        //    line.useWorldSpace = false;
        //    line.loop = false;

        //    line.material = new Material(Shader.Find("Sprites/Default"));
        //    line.startColor = new Color(0f, 1f, 1f, 0.15f);
        //    line.endColor = new Color(0f, 1f, 1f, 0.15f);

        //    float half = size / 2f;
        //    Vector3[] corners = new Vector3[8];
        //    corners[0] = new Vector3(-half, -half, -half); // Lewy Dó³ Przód
        //    corners[1] = new Vector3(half, -half, -half);  // Prawy Dó³ Przód
        //    corners[2] = new Vector3(half, half, -half);   // Prawy Góra Przód
        //    corners[3] = new Vector3(-half, half, -half);  // Lewy Góra Przód
        //    corners[4] = new Vector3(-half, -half, half);  // Lewy Dó³ Ty³
        //    corners[5] = new Vector3(half, -half, half);   // Prawy Dó³ Ty³
        //    corners[6] = new Vector3(half, half, half);    // Prawy Góra Ty³
        //    corners[7] = new Vector3(-half, half, half);   // Lewy Góra Ty³

        //    Vector3[] path = new Vector3[] {
        //        corners[0], corners[1], corners[2], corners[3], corners[0], // Przednia œciana
        //        corners[4], corners[5], corners[1],                         // Dó³
        //        corners[5], corners[6], corners[2],                         // Prawy bok
        //        corners[6], corners[7], corners[3],                         // Góra
        //        corners[7], corners[4]                                      // Ty³ i lewy bok
        //    };

        //    line.positionCount = path.Length;
        //    line.SetPositions(path);
        //}
    }
