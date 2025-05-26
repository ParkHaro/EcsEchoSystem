using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(LifecycleSystem))]
public partial class ReproductionSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        var deltaTime = SystemAPI.Time.DeltaTime;
        var currentTime = (float)SystemAPI.Time.ElapsedTime;
        
        // 번식 가능한 개체들 찾기
        var reproductionJob = new ReproductionJob
        {
            deltaTime = deltaTime,
            currentTime = currentTime,
            ecb = ecb
        };
        
        Dependency = reproductionJob.ScheduleParallel(Dependency);
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

[BurstCompile]
public partial struct ReproductionJob : IJobEntity
{
    public float deltaTime;
    public float currentTime;
    public EntityCommandBuffer.ParallelWriter ecb;
    
    public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex,
                       ref CreatureComponent creature,
                       in LocalTransform transform,
                       in AnimalPersonalityComponent personality)
    {
        // 번식 조건 체크
        if (!CanReproduce(creature))
            return;
            
        // 근처에 같은 종의 반대 성별 개체 찾기
        float mateSearchRange = GetMateSearchRange(creature.animalType);
        
        // 실제 구현에서는 spatial query를 사용해야 하지만
        // 여기서는 단순화된 버전으로 확률 기반 번식
        var random = new Unity.Mathematics.Random((uint)(transform.Position.x * 1000 + currentTime));
        
        // 번식 확률 계산
        float reproductionChance = CalculateReproductionChance(creature, personality);
        
        if (random.NextFloat() < reproductionChance * deltaTime)
        {
            StartPregnancy(ref creature);
        }
    }
    
    private bool CanReproduce(CreatureComponent creature)
    {
        // 기본 번식 조건들
        if (creature.age < creature.reproductionAge) return false;
        if (creature.reproductionCooldown > 0) return false;
        if (creature.isPregnant) return false;
        if (creature.energy < creature.maxEnergy * 0.7f) return false; // 충분한 에너지 필요
        
        return true;
    }
    
    private float GetMateSearchRange(AnimalType animalType)
    {
        switch (animalType)
        {
            case AnimalType.Rabbit: return 5f;
            case AnimalType.Wolf: return 15f;
            case AnimalType.Deer: return 10f;
            default: return 8f;
        }
    }
    
    private float CalculateReproductionChance(CreatureComponent creature, AnimalPersonalityComponent personality)
    {
        float baseChance = GetBaseReproductionChance(creature.animalType);
        
        // 사회성이 높을수록 번식 확률 증가
        baseChance *= (1f + personality.sociability * 0.5f);
        
        // 에너지 상태에 따른 보정
        float energyRatio = creature.energy / creature.maxEnergy;
        baseChance *= energyRatio;
        
        // 나이에 따른 보정 (너무 늙으면 번식력 감소)
        float ageRatio = creature.age / creature.lifespan;
        if (ageRatio > 0.7f)
        {
            baseChance *= (1f - (ageRatio - 0.7f) * 2f);
        }
        
        return math.max(0f, baseChance);
    }
    
    private float GetBaseReproductionChance(AnimalType animalType)
    {
        switch (animalType)
        {
            case AnimalType.Rabbit: return 0.1f; // 높은 번식률
            case AnimalType.Wolf: return 0.02f;  // 낮은 번식률
            case AnimalType.Deer: return 0.05f;  // 중간 번식률
            default: return 0.03f;
        }
    }
    
    private void StartPregnancy(ref CreatureComponent creature)
    {
        creature.isPregnant = true;
        creature.pregnancyTime = GetPregnancyDuration(creature.animalType);
    }
    
    private float GetPregnancyDuration(AnimalType animalType)
    {
        switch (animalType)
        {
            case AnimalType.Rabbit: return 15f;  // 15초
            case AnimalType.Wolf: return 45f;    // 45초
            case AnimalType.Deer: return 30f;    // 30초
            default: return 25f;
        }
    }
}