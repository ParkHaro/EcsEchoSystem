using Unity.Entities;

public enum AIState
{
    Idle,
    Wandering,
    Searching,
    Moving,
    Feeding,
    Drinking,
    Fleeing,
    Hunting,
    Stalking,
    Attacking,
    Mating,
    Territorial,
    Migrating,
    Sleeping,
    Socializing,
    Caring  // 새끼 돌보기
}

[System.Serializable]
public struct AIStateComponent : IComponentData
{
    public AIState currentState;
    public AIState previousState;
    public float stateTimer;
    public float stateChangeCooldown;
    public float statePriority;
    
    // 상태 변경 조건
    public bool canChangeState;
    public AIState desiredState;
}