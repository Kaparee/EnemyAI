using UnityEngine;

[CreateAssetMenu(fileName = "NewAIArchetype", menuName = "AI/Archetype")]
public class AIArchetype : ScriptableObject
{
    public string archetypeCodeName;
    public float maxHealth;
    public float speed;
    public float attackPower;
    public GameObject prefab;
}