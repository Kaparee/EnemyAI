using System.Collections.Generic;
using UnityEngine;

// Ta linijka pozwoli Ci stworzyć bazę kliknięciem w Unity
[CreateAssetMenu(fileName = "NewResourceDatabase", menuName = "Tutorial/Resource Database")]
public class ResourceDatabase : ScriptableObject
{

    [Header("Lista Surowców")]
    public List<ResourceDefinition> Resources = new List<ResourceDefinition>();

    public ResourceDefinition GetRandomResource(int sectorStage)
    {

        float totalWeight = 0f;

        

        foreach (ResourceDefinition res in Resources) {
            totalWeight += res.weightsPerStage[sectorStage];
        }

        if (totalWeight <= 0) {
            return Resources[0];
        }

        float roll = Random.Range(0f, totalWeight);

        foreach (ResourceDefinition res in Resources) {
            if (roll <= res.weightsPerStage[sectorStage]) {
                return res;
            } else {
                roll -= res.weightsPerStage[sectorStage];
            }
        }
        return Resources[0];
    }
    [ContextMenu("Eksportuj do JSON")]
    public void ExportToJSON() {
        string json = JsonUtility.ToJson(this, true);
        string sciezka = Application.dataPath + "/ResourcesExport.json";
        System.IO.File.WriteAllText(sciezka, json);
    }
}