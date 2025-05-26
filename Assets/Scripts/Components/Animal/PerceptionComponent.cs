using Unity.Entities;

[System.Serializable]
public struct PerceptionComponent : IComponentData
{
    public float sightRange;
    public float hearingRange;
    public float smellRange;
    public float fieldOfView;       // 시야각 (degree)
    
    // 감지된 대상들
    public Entity currentTarget;
    public Entity currentThreat;
    public Entity currentMate;
    
    // 감지 상태
    public bool hasTarget;
    public bool inDanger;
    public bool canSeeFood;
    public bool canSeeMate;
}