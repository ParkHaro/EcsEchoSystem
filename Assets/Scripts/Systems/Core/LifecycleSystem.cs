using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public partial class LifecycleSystem : SystemBase
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
        
        // 생명주기 처리 Job
        var lifecycleJob = new LifecycleJob
        {
            deltaTime = deltaTime,
            currentTime = currentTime,
            ecb = ecb
        };
        
        Dependency = lifecycleJob.ScheduleParallel(Dependency);
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}

[BurstCompile]
public partial struct LifecycleJob : IJobEntity
{
    public float deltaTime;
    public float currentTime;
    public EntityCommandBuffer.ParallelWriter ecb;
    
    public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, 
                       ref CreatureComponent creature,
                       in LocalTransform transform)
    {
        // 나이 증가
        creature.age += deltaTime;
        
        // 에너지 자연 소모 (움직임에 따른 추가 소모)
        float energyConsumption = creature.hungerRate * deltaTime;
        
        // 나이에 따른 에너지 소모 증가 (늙을수록 더 많이 소모)
        float ageMultiplier = 1f + (creature.age / creature.lifespan) * 0.5f;
        energyConsumption *= ageMultiplier;
        
        creature.energy = math.max(0f, creature.energy - energyConsumption);
        
        // 임신 처리
        HandlePregnancy(ref creature, entity, chunkIndex, transform);
        
        // 번식 쿨다운 감소
        if (creature.reproductionCooldown > 0)
        {
            creature.reproductionCooldown -= deltaTime;
        }
        
        // 나이별 태그 관리
        ManageAgeTags(entity, chunkIndex, creature);
        
        // 건강 상태 태그 관리
        ManageHealthTags(entity, chunkIndex, creature);
        
        // 죽음 조건 체크
        CheckDeathConditions(entity, chunkIndex, creature);
    }
    
    private void HandlePregnancy(ref CreatureComponent creature, Entity entity, int chunkIndex, 
                               LocalTransform transform)
    {
        if (creature.isPregnant)
        {
            creature.pregnancyTime -= deltaTime;
            
            // 임신 중일 때 에너지 추가 소모
            creature.energy = math.max(0f, creature.energy - 5f * deltaTime);
            
            if (creature.pregnancyTime <= 0)
            {
                // 출산 처리
                GiveBirth(ref creature, entity, chunkIndex, transform);
            }
        }
    }
    
    private void GiveBirth(ref CreatureComponent creature, Entity parent, int chunkIndex, 
                          LocalTransform transform)
    {
        // 출산 완료
        creature.isPregnant = false;
        creature.reproductionCooldown = GetReproductionCooldown(creature.animalType);
        
        // 출산으로 인한 에너지 소모
        creature.energy = math.max(10f, creature.energy * 0.6f);
        
        // 새끼 개수 결정 (동물 종류에 따라)
        int offspring = GetOffspringCount(creature.animalType);
        
        for (int i = 0; i < offspring; i++)
        {
            CreateOffspring(parent, chunkIndex, transform, creature.animalType);
        }
    }
    
    private void CreateOffspring(Entity parent, int chunkIndex, LocalTransform transform, 
                               AnimalType animalType)
    {
        // 새 Entity 생성
        Entity offspring = ecb.Instantiate(chunkIndex, parent);
        
        // 새끼 위치 설정 (부모 근처)
        var random = new Unity.Mathematics.Random((uint)(transform.Position.x * 1000 + currentTime * 1000));
        float3 offset = random.NextFloat3(-2f, 2f);
        offset.y = 0;
        
        ecb.SetComponent(chunkIndex, offspring, LocalTransform.FromPosition(transform.Position + offset));
        
        // 새끼 초기 설정
        var offspringCreature = GetOffspringCreatureComponent(animalType);
        ecb.SetComponent(chunkIndex, offspring, offspringCreature);
        
        // 새끼 태그 추가
        ecb.AddComponent<YoungTag>(chunkIndex, offspring);
    }
    
    private CreatureComponent GetOffspringCreatureComponent(AnimalType animalType)
    {
        var random = new Unity.Mathematics.Random((uint)(currentTime * 1000));
        
        switch (animalType)
        {
            case AnimalType.Rabbit:
                return new CreatureComponent
                {
                    animalType = AnimalType.Rabbit,
                    gender = random.NextBool() ? Gender.Male : Gender.Female,
                    energy = 60f,
                    maxEnergy = 100f,
                    hungerRate = 6f, // 새끼는 배고픔이 더 빠름
                    age = 0f,
                    lifespan = random.NextFloat(8f, 12f),
                    reproductionAge = 2f,
                    size = 0.15f, // 작은 크기
                    weight = 1f,
                    speed = 4f, // 느린 속도
                    isPregnant = false,
                    pregnancyTime = 0f,
                    reproductionCooldown = 0f
                };
                
            case AnimalType.Wolf:
                return new CreatureComponent
                {
                    animalType = AnimalType.Wolf,
                    gender = random.NextBool() ? Gender.Male : Gender.Female,
                    energy = 80f,
                    maxEnergy = 150f,
                    hungerRate = 4f,
                    age = 0f,
                    lifespan = random.NextFloat(12f, 18f),
                    reproductionAge = 3f,
                    size = 0.6f,
                    weight = 20f,
                    speed = 5f,
                    isPregnant = false,
                    pregnancyTime = 0f,
                    reproductionCooldown = 0f
                };
                
            case AnimalType.Deer:
                return new CreatureComponent
                {
                    animalType = AnimalType.Deer,
                    gender = random.NextBool() ? Gender.Male : Gender.Female,
                    energy = 70f,
                    maxEnergy = 120f,
                    hungerRate = 5f,
                    age = 0f,
                    lifespan = random.NextFloat(10f, 15f),
                    reproductionAge = 2.5f,
                    size = 0.5f,
                    weight = 40f,
                    speed = 7f,
                    isPregnant = false,
                    pregnancyTime = 0f,
                    reproductionCooldown = 0f
                };
                
            default:
                return new CreatureComponent(); // 기본값
        }
    }
    
    private float GetReproductionCooldown(AnimalType animalType)
    {
        switch (animalType)
        {
            case AnimalType.Rabbit:
                return 30f; // 30초
            case AnimalType.Wolf:
                return 120f; // 2분
            case AnimalType.Deer:
                return 90f; // 1분 30초
            default:
                return 60f;
        }
    }
    
    private int GetOffspringCount(AnimalType animalType)
    {
        var random = new Unity.Mathematics.Random((uint)(currentTime * 1000));
        
        switch (animalType)
        {
            case AnimalType.Rabbit:
                return random.NextInt(2, 6); // 2-5마리
            case AnimalType.Wolf:
                return random.NextInt(1, 4); // 1-3마리
            case AnimalType.Deer:
                return random.NextInt(1, 3); // 1-2마리
            default:
                return 1;
        }
    }
    
    private void ManageAgeTags(Entity entity, int chunkIndex, CreatureComponent creature)
    {
        float ageRatio = creature.age / creature.lifespan;
        
        // 새끼 태그 관리
        if (ageRatio < 0.2f) // 생애 20% 미만이면 새끼
        {
            ecb.AddComponent<YoungTag>(chunkIndex, entity);
        }
        else
        {
            ecb.RemoveComponent<YoungTag>(chunkIndex, entity);
        }
        
        // 늙음 태그 관리
        if (ageRatio > 0.8f) // 생애 80% 이상이면 늙음
        {
            ecb.AddComponent<OldTag>(chunkIndex, entity);
        }
        else
        {
            ecb.RemoveComponent<OldTag>(chunkIndex, entity);
        }
    }
    
    private void ManageHealthTags(Entity entity, int chunkIndex, CreatureComponent creature)
    {
        float energyRatio = creature.energy / creature.maxEnergy;
        
        // 배고픔 태그 관리
        if (energyRatio < 0.3f)
        {
            ecb.AddComponent<HungryTag>(chunkIndex, entity);
        }
        else
        {
            ecb.RemoveComponent<HungryTag>(chunkIndex, entity);
        }
        
        // 피로 태그 관리 (에너지가 매우 낮을 때)
        if (energyRatio < 0.1f)
        {
            ecb.AddComponent<TiredTag>(chunkIndex, entity);
        }
        else
        {
            ecb.RemoveComponent<TiredTag>(chunkIndex, entity);
        }
    }
    
    private void CheckDeathConditions(Entity entity, int chunkIndex, CreatureComponent creature)
    {
        bool shouldDie = false;
        
        // 수명 다함
        if (creature.age >= creature.lifespan)
        {
            shouldDie = true;
        }
        
        // 굶어 죽음
        if (creature.energy <= 0)
        {
            shouldDie = true;
        }
        
        // 새끼가 너무 어릴 때 에너지 부족으로 죽음 (현실적)
        if (creature.age < 1f && creature.energy < 10f)
        {
            shouldDie = true;
        }
        
        if (shouldDie)
        {
            // 죽음 처리 - 먹이로 변환하거나 그냥 제거
            HandleDeath(entity, chunkIndex, creature);
        }
    }
    
    private void HandleDeath(Entity entity, int chunkIndex, CreatureComponent creature)
    {
        // 육식동물이 죽으면 다른 동물들의 먹이가 될 수 있음
        if (creature.animalType == AnimalType.Wolf)
        {
            // 늑대 시체는 청소동물의 먹이가 될 수 있음 (추후 구현)
        }
        
        // Entity 제거
        ecb.DestroyEntity(chunkIndex, entity);
    }
}