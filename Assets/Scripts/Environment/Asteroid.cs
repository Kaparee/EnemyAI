using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;

public class Asteroid : MonoBehaviour
{
    public List<ResourceStack> materials = new List<ResourceStack>();
    //public TextMeshPro info;


    private List<float> GetWeightedTemps()
    {
        float sum = materials.Sum(m => m.amount);
        return materials
            .Select(m => (float)m.definition.optimalTemp * (1f + (float)m.amount / sum))
            .ToList();
    }


    public float ToleranceTemperature()
    {
        var temps = GetWeightedTemps();
        return Mathf.Ceil(temps.Min() * 0.8f + temps.Average() * 0.2f);
    }

    public float CalculateTemperature()
    {
        var temps = GetWeightedTemps();
        return Mathf.Ceil(temps.Max() * 0.8f + temps.Average() * 0.2f);
    }


    //public void showInfoAsteroid(){
    //    if (info == null) return;

    //    // Debug ¿eby nie wyœwietla³o NAN ani pustych list
    //    if (materials == null || materials.Count == 0) {
    //        info.text = "Skanowanie...";
    //        return;
    //    }

    //    string textInfo = $"Temperatura: {CalculateTemperature()} \u00B0C" +
    //    $"\nTolerancja: {ToleranceTemperature()} \u00B0C" +
    //    $"\nSurowce:\n";
    //    foreach(var m in materials){
    //        textInfo += $"{m.definition.Name} - {m.amount}\n";
    //    }
    //    info.text = textInfo;
    //}

    void Start(){
        //showInfoAsteroid();
    }
}
