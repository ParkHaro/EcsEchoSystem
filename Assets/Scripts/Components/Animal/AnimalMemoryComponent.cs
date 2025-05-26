using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public struct MemoryLocation
{
    public float3 position;
    public float timestamp;
    public MemoryType type;
}

public enum MemoryType
{
    Food,
    Danger,
    Water,
    Shelter,
    Mate
}

[System.Serializable]
public struct AnimalMemoryComponent : IComponentData
{
    public float memoryDuration;
    public int maxMemoryCount;
    // Note: NativeList는 별도로 관리해야 함
}