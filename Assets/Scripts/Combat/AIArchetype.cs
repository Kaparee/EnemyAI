using UnityEngine;

[CreateAssetMenu(fileName = "NewAIArchetype", menuName = "AI/Archetype")]
// Definiuje warianty zachowań i parametry poszczególnych archetypów przeciwników, ułatwiając konfigurację różnorodnych jednostek.
public class AIArchetype : ScriptableObject
{
    public string archetypeCodeName;
    public float maxHealth;
    public float speed;
    public float attackPower;
    public GameObject prefab;
}