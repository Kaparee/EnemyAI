using System;
using UnityEngine;

[Serializable]
// ScriptableObject definiujący parametry konkretnego zasobu (np. jego ikonę i limit w pojedynczym stosie ekwipunku).
public class ResourceDefinition
{
    public string Name;
    public int Stage;
    public float optimalTemp;
    public float tolerance;
    public float[] weightsPerStage = new float[5];
    public int basePrice;
    public Sprite Icon;
    public float weight;
} 
