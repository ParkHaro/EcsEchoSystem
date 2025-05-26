using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(TransformSystemGroup))]
public partial class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        // 모든 움직이는 Entity에 대해 이동 처리
        var movementJob = new MovementJob
        {
            deltaTime = deltaTime
        };
        
        Dependency = movementJob.ScheduleParallel(Dependency);
    }
}

[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    public float deltaTime;
    
    public void Execute(ref LocalTransform transform, ref MovementComponent movement)
    {
        // 목표가 있으면 목표 방향으로 이동
        if (movement.hasTarget)
        {
            HandleTargetedMovement(ref transform, ref movement);
        }
        
        // 물리 기반 이동 적용
        ApplyMovement(ref transform, ref movement);
        
        // 이동 상태 업데이트
        UpdateMovementState(ref movement, transform);
    }
    
    private void HandleTargetedMovement(ref LocalTransform transform, ref MovementComponent movement)
    {
        float3 currentPosition = transform.Position;
        float3 targetDirection = movement.targetPosition - currentPosition;
        float distanceToTarget = math.length(targetDirection);
        
        // 목표에 도달했는지 체크
        if (distanceToTarget < 0.5f)
        {
            movement.hasTarget = false;
            movement.velocity = float3.zero;
            movement.isMoving = false;
            return;
        }
        
        // 목표 방향으로의 원하는 속도 계산
        float3 desiredVelocity = math.normalize(targetDirection) * movement.maxSpeed;
        
        // 이동 패턴에 따른 속도 조정
        desiredVelocity = ApplyMovementPattern(desiredVelocity, movement, currentPosition);
        
        // 스무스한 가속/감속을 위한 보간
        float accelerationRate = 5f * deltaTime;
        movement.velocity = math.lerp(movement.velocity, desiredVelocity, accelerationRate);
        
        movement.currentSpeed = math.length(movement.velocity);
        movement.isMoving = movement.currentSpeed > 0.1f;
        
        // 회전 처리
        if (movement.isMoving)
        {
            UpdateRotation(ref transform, movement);
        }
    }
    
    private float3 ApplyMovementPattern(float3 baseVelocity, MovementComponent movement, float3 currentPosition)
    {
        switch (movement.pattern)
        {
            case MovementPattern.Flee:
                return ApplyFleePattern(baseVelocity, movement);
                
            case MovementPattern.Follow:
                return ApplyFollowPattern(baseVelocity, movement);
                
            case MovementPattern.Patrol:
                return ApplyPatrolPattern(baseVelocity, movement, currentPosition);
                
            default:
                return baseVelocity;
        }
    }
    
    private float3 ApplyZigzagPattern(float3 baseVelocity, MovementComponent movement)
    {
        // 지그재그 움직임 - 좌우로 흔들리는 패턴
        float zigzagFrequency = 2f;
        float zigzagAmplitude = 0.3f;
        
        float3 right = math.cross(baseVelocity, math.up());
        float zigzagOffset = math.sin(movement.patternTimer * zigzagFrequency) * zigzagAmplitude;
        
        return baseVelocity + right * zigzagOffset;
    }
    
    private float3 ApplyCirclePattern(float3 baseVelocity, MovementComponent movement, float3 currentPosition)
    {
        // 원형 움직임
        float circleRadius = 5f;
        float angularSpeed = 1f;
        
        float angle = movement.patternTimer * angularSpeed;
        float3 center = movement.targetPosition;
        
        float3 circlePosition = center + new float3(
            math.cos(angle) * circleRadius,
            0,
            math.sin(angle) * circleRadius
        );
        
        return math.normalize(circlePosition - currentPosition) * math.length(baseVelocity);
    }
    
    private float3 ApplyRandomPattern(float3 baseVelocity, MovementComponent movement, float3 currentPosition)
    {
        // 랜덤한 방향 변경
        var random = new Unity.Mathematics.Random((uint)(currentPosition.x * 1000 + movement.patternTimer * 100));
        
        if (movement.patternTimer % 1f < deltaTime) // 1초마다 방향 변경
        {
            float3 randomDirection = random.NextFloat3Direction();
            randomDirection.y = 0; // Y축 제한
            return math.normalize(randomDirection) * math.length(baseVelocity);
        }
        
        return baseVelocity;
    }
    
    private float3 ApplyFleePattern(float3 baseVelocity, MovementComponent movement)
    {
        // 도망 패턴 - 속도 증가 및 불규칙적 움직임
        float3 fleeVelocity = baseVelocity * 1.5f; // 속도 50% 증가
        
        // 약간의 불규칙성 추가 (공포로 인한)
        var random = new Unity.Mathematics.Random((uint)(movement.patternTimer * 1000));
        float3 noise = random.NextFloat3(-0.2f, 0.2f);
        noise.y = 0;
        
        return fleeVelocity + noise;
    }
    
    private float3 ApplyFollowPattern(float3 baseVelocity, MovementComponent movement)
    {
        // 추적 패턴 - 더 정확하고 끈질긴 추적
        return baseVelocity * 1.2f; // 속도 20% 증가
    }
    
    private float3 ApplyPatrolPattern(float3 baseVelocity, MovementComponent movement, float3 currentPosition)
    {
        // 순찰 패턴 - 일정한 속도로 정해진 경로
        return math.normalize(baseVelocity) * (movement.maxSpeed * 0.7f); // 70% 속도로 순찰
    }
    
    private void ApplyMovement(ref LocalTransform transform, ref MovementComponent movement)
    {
        // 실제 위치 업데이트
        movement.lastPosition = transform.Position;
        transform.Position += movement.velocity * deltaTime;
        
        // 패턴 타이머 업데이트
        movement.patternTimer += deltaTime;
        
        // 속도 제한
        movement.currentSpeed = math.min(movement.currentSpeed, movement.maxSpeed);
        
        // 지면에 고정 (Y축 제한)
        float3 position = transform.Position;
        position.y = 0;
        transform.Position = position;
    }
    
    private void UpdateRotation(ref LocalTransform transform, MovementComponent movement)
    {
        if (math.length(movement.velocity) > 0.1f)
        {
            // 이동 방향을 향해 회전
            float3 forward = math.normalize(movement.velocity);
            quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
            
            // 부드러운 회전
            float rotationSpeed = movement.rotationSpeed * deltaTime;
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed);
        }
    }
    
    private void UpdateMovementState(ref MovementComponent movement, LocalTransform transform)
    {
        // 이동 거리 계산
        float distanceMoved = math.distance(movement.lastPosition, transform.Position);
        movement.isMoving = distanceMoved > 0.01f;
        
        // 속도 계산
        if (deltaTime > 0)
        {
            movement.currentSpeed = distanceMoved / deltaTime;
        }
    }
}