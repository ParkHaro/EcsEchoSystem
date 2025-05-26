using Unity.Entities;

[System.Serializable]
public struct AnimalPersonalityComponent : IComponentData
{
    public float aggressiveness;    // 공격성 (0-1)
    public float curiosity;         // 호기심 (0-1)
    public float fearfulness;       // 겁이 많은 정도 (0-1)
    public float sociability;       // 사회성 (0-1)
    public float territoriality;    // 영역성 (0-1)
    public float intelligence;      // 지능 (0-1)
    public float dominance;         // 우월성 (0-1)
    public float adaptability;      // 적응력 (0-1)
}