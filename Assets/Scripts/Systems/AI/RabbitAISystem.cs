using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial class RabbitAISystem : SystemBase
{
    private EntityQuery rabbitQuery;
    private EntityQuery predatorQuery;
    private EntityQuery foodQuery;
    
    protected override void OnCreate()
    {
        // 토끼 쿼리 생성
        rabbitQuery = GetEntityQuery(
            ComponentType.ReadWrite<AIStateComponent>(),
            ComponentType.ReadWrite<MovementComponent>(),
            ComponentType.ReadWrite<CreatureComponent>(),
            ComponentType.ReadOnly<AnimalPersonalityComponent>(),
            ComponentType.ReadOnly<PerceptionComponent>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<RabbitTag>()
        );
        
        // 포식자 쿼리
        predatorQuery = GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<PredatorTag>()
        );
        
        // 음식 쿼리
        foodQuery = GetEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<FoodSourceComponent>()
        );
    }
    
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var predatorPositions = predatorQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var foodPositions = foodQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        
        // 토끼 AI 행동 처리
        var rabbitAIJob = new RabbitAIJob
        {
            deltaTime = deltaTime,
            predatorPositions = predatorPositions,
            foodPositions = foodPositions,
            time = (float)SystemAPI.Time.ElapsedTime
        };
        
        Dependency = rabbitAIJob.ScheduleParallel(rabbitQuery, Dependency);
        
        predatorPositions.Dispose(Dependency);
        foodPositions.Dispose(Dependency);
    }
}

[BurstCompile]
public partial struct RabbitAIJob : IJobEntity
{
    public float deltaTime;
    public float time;
    
    [ReadOnly] public NativeArray<LocalTransform> predatorPositions;
    [ReadOnly] public NativeArray<LocalTransform> foodPositions;
    
    public void Execute(
        ref AIStateComponent aiState,
        ref MovementComponent movement,
        ref CreatureComponent creature,
        in AnimalPersonalityComponent personality,
        in PerceptionComponent perception,
        in LocalTransform transform)
    {
        // 상태 타이머 업데이트
        aiState.stateTimer += deltaTime;
        
        // 위험 감지가 최우선
        if (DetectDanger(transform.Position, perception, aiState.currentState))
        {
            HandleFleeing(ref aiState, ref movement, transform.Position, personality);
            return;
        }
        
        // 현재 상태에 따른 행동 처리
        switch (aiState.currentState)
        {
            case AIState.Idle:
                HandleIdle(ref aiState, ref movement, ref creature, personality, transform.Position);
                break;
                
            case AIState.Wandering:
                HandleWandering(ref aiState, ref movement, transform.Position);
                break;
                
            case AIState.Searching:
                HandleSearching(ref aiState, ref movement, transform.Position, perception);
                break;
                
            case AIState.Feeding:
                HandleFeeding(ref aiState, ref creature, transform.Position);
                break;
                
            case AIState.Fleeing:
                HandleFleeing(ref aiState, ref movement, transform.Position, personality);
                break;
        }
        
        // 기본 욕구 체크
        CheckBasicNeeds(ref aiState, ref creature, personality);
    }
    
    private bool DetectDanger(float3 position, PerceptionComponent perception, AIState currentState)
    {
        // 이미 도망치고 있으면 계속 체크
        for (int i = 0; i < predatorPositions.Length; i++)
        {
            float distance = math.distance(position, predatorPositions[i].Position);
            
            // 시야 범위 내에 포식자가 있으면 위험
            if (distance < perception.sightRange)
            {
                return true;
            }
            
            // 더 가까우면 더 민감하게 반응
            if (distance < perception.sightRange * 0.5f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void HandleIdle(ref AIStateComponent aiState, ref MovementComponent movement, 
                           ref CreatureComponent creature, AnimalPersonalityComponent personality, 
                           float3 position)
    {
        // 토끼는 오래 가만히 있지 않음 (경계심 때문에)
        float maxIdleTime = math.lerp(1f, 3f, 1f - personality.fearfulness);
        
        if (aiState.stateTimer > maxIdleTime)
        {
            // 배고프면 음식 찾기, 아니면 배회
            if (creature.energy < creature.maxEnergy * 0.6f)
            {
                ChangeState(ref aiState, AIState.Searching);
            }
            else
            {
                // 호기심에 따라 탐험 vs 안전한 배회
                var random = new Unity.Mathematics.Random((uint)(position.x * 1000 + time * 1000));
                if (random.NextFloat() < personality.curiosity)
                {
                    ChangeState(ref aiState, AIState.Searching);
                }
                else
                {
                    ChangeState(ref aiState, AIState.Wandering);
                    SetRandomTarget(ref movement, position, 5f);
                }
            }
        }
    }
    
    private void HandleWandering(ref AIStateComponent aiState, ref MovementComponent movement, 
                                float3 position)
    {
        // 목표 지점에 도달했거나 시간이 지나면 새로운 행동
        if (!movement.hasTarget || aiState.stateTimer > 8f)
        {
            ChangeState(ref aiState, AIState.Idle);
        }
        
        // 가끔 방향 바꾸기 (토끼의 불규칙한 움직임)
        if (aiState.stateTimer % 2f < deltaTime)
        {
            var random = new Unity.Mathematics.Random((uint)(position.x * 1000 + time * 1000));
            if (random.NextFloat() < 0.3f) // 30% 확률로 방향 변경
            {
                SetRandomTarget(ref movement, position, 3f);
            }
        }
    }
    
    private void HandleSearching(ref AIStateComponent aiState, ref MovementComponent movement,
                                float3 position, PerceptionComponent perception)
    {
        // 가장 가까운 음식 찾기
        float closestFoodDistance = float.MaxValue;
        float3 closestFoodPosition = float3.zero;
        bool foundFood = false;
        
        for (int i = 0; i < foodPositions.Length; i++)
        {
            float distance = math.distance(position, foodPositions[i].Position);
            
            if (distance < perception.sightRange && distance < closestFoodDistance)
            {
                closestFoodDistance = distance;
                closestFoodPosition = foodPositions[i].Position;
                foundFood = true;
            }
        }
        
        if (foundFood)
        {
            // 음식 발견하면 이동
            movement.targetPosition = closestFoodPosition;
            movement.hasTarget = true;
            
            // 가까이 가면 먹기 시작
            if (closestFoodDistance < 1f)
            {
                ChangeState(ref aiState, AIState.Feeding);
            }
        }
        else
        {
            // 음식을 못 찾으면 랜덤하게 이동
            if (!movement.hasTarget)
            {
                SetRandomTarget(ref movement, position, 10f);
            }
            
            // 너무 오래 찾으면 포기하고 배회
            if (aiState.stateTimer > 15f)
            {
                ChangeState(ref aiState, AIState.Wandering);
                SetRandomTarget(ref movement, position, 5f);
            }
        }
    }
    
    private void HandleFeeding(ref AIStateComponent aiState, ref CreatureComponent creature, 
                              float3 position)
    {
        // 먹이를 먹으면서 에너지 회복
        creature.energy = math.min(creature.maxEnergy, creature.energy + 20f * deltaTime);
        
        // 충분히 먹었거나 시간이 지나면 끝
        if (creature.energy > creature.maxEnergy * 0.9f || aiState.stateTimer > 5f)
        {
            ChangeState(ref aiState, AIState.Idle);
        }
    }
    
    private void HandleFleeing(ref AIStateComponent aiState, ref MovementComponent movement,
                              float3 position, AnimalPersonalityComponent personality)
    {
        // 가장 가까운 위험으로부터 도망
        float3 dangerDirection = float3.zero;
        float closestDangerDistance = float.MaxValue;
        bool inDanger = false;
        
        for (int i = 0; i < predatorPositions.Length; i++)
        {
            float distance = math.distance(position, predatorPositions[i].Position);
            
            if (distance < closestDangerDistance)
            {
                closestDangerDistance = distance;
                dangerDirection = math.normalize(predatorPositions[i].Position - position);
                inDanger = distance < 15f; // 15m 이내면 위험
            }
        }
        
        if (inDanger)
        {
            // 위험 반대 방향으로 도망
            float3 fleeDirection = -dangerDirection;
            movement.targetPosition = position + fleeDirection * 25f;
            movement.hasTarget = true;
            movement.currentSpeed = movement.maxSpeed * 1.5f; // 도망칠 때 속도 증가
            
            // 상태 갱신 - 계속 도망
            ChangeState(ref aiState, AIState.Fleeing);
        }
        else
        {
            // 안전해지면 경계 상태로
            movement.currentSpeed = movement.maxSpeed; // 속도 정상화
            ChangeState(ref aiState, AIState.Idle);
        }
    }
    
    private void CheckBasicNeeds(ref AIStateComponent aiState, ref CreatureComponent creature,
                                AnimalPersonalityComponent personality)
    {
        // 에너지가 부족하면 음식 찾기 우선
        if (creature.energy < creature.maxEnergy * 0.3f && 
            aiState.currentState != AIState.Fleeing && 
            aiState.currentState != AIState.Feeding)
        {
            ChangeState(ref aiState, AIState.Searching);
        }
        
        // 에너지 자연 소모
        creature.energy = math.max(0f, creature.energy - creature.hungerRate * deltaTime);
        
        // 나이 증가
        creature.age += deltaTime;
    }
    
    private void ChangeState(ref AIStateComponent aiState, AIState newState)
    {
        if (aiState.currentState != newState)
        {
            aiState.previousState = aiState.currentState;
            aiState.currentState = newState;
            aiState.stateTimer = 0f;
        }
    }
    
    private void SetRandomTarget(ref MovementComponent movement, float3 currentPosition, float range)
    {
        var random = new Unity.Mathematics.Random((uint)(currentPosition.x * 1000 + time * 1000));
        float3 randomDirection = random.NextFloat3Direction();
        randomDirection.y = 0; // Y축 이동 제한
        
        movement.targetPosition = currentPosition + randomDirection * range;
        movement.hasTarget = true;
    }
}