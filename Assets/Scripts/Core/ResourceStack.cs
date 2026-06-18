using System;

[Serializable]
// Skrypt ułatwiający składowanie wielu takich samych elementów w stosach (podstawa systemu Ekwipunku).
public class ResourceStack {
    public ResourceDefinition definition;
    public int amount;
}