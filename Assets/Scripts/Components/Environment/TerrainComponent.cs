using Unity.Entities;

public enum TerrainType
{
    Grassland,
    Forest,
    Mountain,
    River,
    Desert,
    Swamp
}

[System.Serializable]
public struct TerrainComponent : IComponentData
{
    public TerrainType terrainType;
    public float movementModifier;  // 이동 속도 배율
    public float visibilityModifier; // 시야 거리 배율
    public float shelterValue;      // 은신처 가치
    public bool providesWater;
    public bool providesFood;
}