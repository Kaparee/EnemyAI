using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Asteroid : MonoBehaviour
{
    public List<ResourceStack> materials = new List<ResourceStack>();

    private List<float> GetWeightedTemps()
    {
        float sum = (float)materials.Sum(m => m.amount);
        if (sum == 0) return new List<float> { 0f };

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

    void Start()
    {
    }
}
