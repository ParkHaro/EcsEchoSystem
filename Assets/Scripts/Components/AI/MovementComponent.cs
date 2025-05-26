using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct MovementComponent : IComponentData
{
    public float3 velocity;
    public float3 acceleration;
    public float3 targetPosition;
    public float3 lastPosition;
    
    public float currentSpeed;
    public float maxSpeed;
    public float rotationSpeed;
    
    public bool hasTarget;
    public bool isMoving;
    public bool avoidObstacles;
    
    // 이동 패턴
    public MovementPattern pattern;
    public float patternTimer;
}

public enum MovementPattern
{
    Direct,     // 직선 이동
    Zigzag,     // 지그재그
    Circle,     // 원형
    Random,     // 랜덤
    Follow,     // 추적
    Flee,       // 도망
    Patrol      // 순찰
}