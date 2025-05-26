using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class EcosystemManager : MonoBehaviour
{
    [Header("생태계 초기 설정")]
    [SerializeField] private int initialRabbitCount = 50;
    [SerializeField] private int initialWolfCount = 8;
    [SerializeField] private int initialDeerCount = 30;
    [SerializeField] private int initialFoodCount = 200;
    
    [Header("지역 설정")]
    [SerializeField] private float ecosystemSize = 100f;
    [SerializeField] private Vector3 ecosystemCenter = Vector3.zero;
    
    [Header("스폰 설정")]
    [SerializeField] private GameObject rabbitPrefab;
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private GameObject deerPrefab;
    [SerializeField] private GameObject grassPrefab;
    
    [Header("실시간 모니터링")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool enableAutoBalance = true;
    
    private EntityManager entityManager;
    private World world;
    
    // 통계 추적
    public EcosystemStats currentStats = new EcosystemStats();
    
    void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        
        InitializeEcosystem();
        
        if (enableAutoBalance)
        {
            InvokeRepeating(nameof(UpdateEcosystemBalance), 10f, 10f);
        }
    }
    
    void Update()
    {
        UpdateStatistics();
        
        if (showDebugInfo)
        {
            DisplayDebugInfo();
        }
    }
    
    private void InitializeEcosystem()
    {
        Debug.Log("🌱 생태계 초기화 시작...");
        
        // 먹이 생성 (가장 먼저)
        SpawnFood(initialFoodCount);
        
        // 초식동물 생성
        SpawnAnimals(AnimalType.Rabbit, initialRabbitCount);
        SpawnAnimals(AnimalType.Deer, initialDeerCount);
        
        // 육식동물 생성 (마지막)
        SpawnAnimals(AnimalType.Wolf, initialWolfCount);
        
        Debug.Log($"🌍 생태계 초기화 완료! 토끼: {initialRabbitCount}, 사슴: {initialDeerCount}, 늑대: {initialWolfCount}, 먹이: {initialFoodCount}");
    }
    
    private void SpawnAnimals(AnimalType animalType, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            CreateAnimal(animalType, spawnPosition);
        }
    }
    
    private Entity CreateAnimal(AnimalType animalType, Vector3 position)
    {
        Entity entity = entityManager.CreateEntity();
        
        // 공통 컴포넌트 추가
        entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
        
        // 동물 종류별 설정
        switch (animalType)
        {
            case AnimalType.Rabbit:
                SetupRabbit(entity);
                break;
            case AnimalType.Wolf:
                SetupWolf(entity);
                break;
            case AnimalType.Deer:
                SetupDeer(entity);
                break;
        }
        
        return entity;
    }
    
    private void SetupRabbit(Entity entity)
    {
        // 기본 생물 컴포넌트
        entityManager.AddComponentData(entity, new CreatureComponent
        {
            animalType = AnimalType.Rabbit,
            gender = UnityEngine.Random.value > 0.5f ? Gender.Male : Gender.Female,
            energy = 80f,
            maxEnergy = 100f,
            hungerRate = 8f,
            age = 0f,
            lifespan = UnityEngine.Random.Range(8f, 12f),
            reproductionAge = 2f,
            size = 0.3f,
            weight = 2f,
            speed = 6f,
            isPregnant = false,
            pregnancyTime = 0f,
            reproductionCooldown = 0f
        });
        
        // 성격 특성 (토끼 - 겁 많고 호기심 많음)
        entityManager.AddComponentData(entity, new AnimalPersonalityComponent
        {
            aggressiveness = 0.1f,
            curiosity = 0.7f,
            fearfulness = 0.9f,
            sociability = 0.4f,
            territoriality = 0.2f,
            intelligence = 0.4f,
            dominance = 0.2f,
            adaptability = 0.8f
        });
        
        // 감지 능력
        entityManager.AddComponentData(entity, new PerceptionComponent
        {
            sightRange = 12f,
            hearingRange = 18f,
            smellRange = 6f,
            fieldOfView = 300f,
            hasTarget = false,
            inDanger = false,
            canSeeFood = false,
            canSeeMate = false
        });
        
        // AI 상태
        entityManager.AddComponentData(entity, new AIStateComponent
        {
            currentState = AIState.Idle,
            previousState = AIState.Idle,
            stateTimer = 0f,
            stateChangeCooldown = 0f,
            statePriority = 1f,
            canChangeState = true,
            desiredState = AIState.Idle
        });
        
        // 이동 컴포넌트
        entityManager.AddComponentData(entity, new MovementComponent
        {
            velocity = float3.zero,
            acceleration = float3.zero,
            targetPosition = float3.zero,
            lastPosition = float3.zero,
            currentSpeed = 0f,
            maxSpeed = 6f,
            rotationSpeed = 8f,
            hasTarget = false,
            isMoving = false,
            avoidObstacles = true,
            pattern = MovementPattern.Random,
            patternTimer = 0f
        });
        
        // 태그 추가
        entityManager.AddComponent<RabbitTag>(entity);
        entityManager.AddComponent<HerbivoreTag>(entity);
        entityManager.AddComponent<PreyTag>(entity);
    }
    
    private void SetupWolf(Entity entity)
    {
        // 늑대 설정
        entityManager.AddComponentData(entity, new CreatureComponent
        {
            animalType = AnimalType.Wolf,
            gender = UnityEngine.Random.value > 0.5f ? Gender.Male : Gender.Female,
            energy = 120f,
            maxEnergy = 150f,
            hungerRate = 5f,
            age = 0f,
            lifespan = UnityEngine.Random.Range(12f, 18f),
            reproductionAge = 3f,
            size = 1.2f,
            weight = 40f,
            speed = 8f,
            isPregnant = false,
            pregnancyTime = 0f,
            reproductionCooldown = 0f
        });
        
        // 늑대 성격 (공격적이고 지능적)
        entityManager.AddComponentData(entity, new AnimalPersonalityComponent
        {
            aggressiveness = 0.8f,
            curiosity = 0.6f,
            fearfulness = 0.2f,
            sociability = 0.7f,
            territoriality = 0.9f,
            intelligence = 0.9f,
            dominance = 0.7f,
            adaptability = 0.6f
        });
        
        // 늑대 감지 능력 (뛰어남)
        entityManager.AddComponentData(entity, new PerceptionComponent
        {
            sightRange = 25f,
            hearingRange = 30f,
            smellRange = 35f,
            fieldOfView = 180f,
            hasTarget = false,
            inDanger = false,
            canSeeFood = false,
            canSeeMate = false
        });
        
        // AI 상태
        entityManager.AddComponentData(entity, new AIStateComponent
        {
            currentState = AIState.Wandering,
            previousState = AIState.Idle,
            stateTimer = 0f,
            stateChangeCooldown = 0f,
            statePriority = 1f,
            canChangeState = true,
            desiredState = AIState.Wandering
        });
        
        // 이동 컴포넌트
        entityManager.AddComponentData(entity, new MovementComponent
        {
            velocity = float3.zero,
            acceleration = float3.zero,
            targetPosition = float3.zero,
            lastPosition = float3.zero,
            currentSpeed = 0f,
            maxSpeed = 8f,
            rotationSpeed = 6f,
            hasTarget = false,
            isMoving = false,
            avoidObstacles = true,
            pattern = MovementPattern.Patrol,
            patternTimer = 0f
        });
        
        // 태그 추가
        entityManager.AddComponent<WolfTag>(entity);
        entityManager.AddComponent<CarnivoreTag>(entity);
        entityManager.AddComponent<PredatorTag>(entity);
    }
    
    private void SetupDeer(Entity entity)
    {
        // 사슴 설정
        entityManager.AddComponentData(entity, new CreatureComponent
        {
            animalType = AnimalType.Deer,
            gender = UnityEngine.Random.value > 0.5f ? Gender.Male : Gender.Female,
            energy = 100f,
            maxEnergy = 120f,
            hungerRate = 6f,
            age = 0f,
            lifespan = UnityEngine.Random.Range(10f, 15f),
            reproductionAge = 2.5f,
            size = 1.0f,
            weight = 80f,
            speed = 10f,
            isPregnant = false,
            pregnancyTime = 0f,
            reproductionCooldown = 0f
        });
        
        // 사슴 성격 (사회적이고 경계심 많음)
        entityManager.AddComponentData(entity, new AnimalPersonalityComponent
        {
            aggressiveness = 0.2f,
            curiosity = 0.5f,
            fearfulness = 0.7f,
            sociability = 0.8f,
            territoriality = 0.1f,
            intelligence = 0.6f,
            dominance = 0.3f,
            adaptability = 0.7f
        });
        
        // 사슴 감지 능력
        entityManager.AddComponentData(entity, new PerceptionComponent
        {
            sightRange = 20f,
            hearingRange = 25f,
            smellRange = 10f,
            fieldOfView = 270f,
            hasTarget = false,
            inDanger = false,
            canSeeFood = false,
            canSeeMate = false
        });
        
        // AI 상태
        entityManager.AddComponentData(entity, new AIStateComponent
        {
            currentState = AIState.Searching,
            previousState = AIState.Idle,
            stateTimer = 0f,
            stateChangeCooldown = 0f,
            statePriority = 1f,
            canChangeState = true,
            desiredState = AIState.Searching
        });
        
        // 이동 컴포넌트
        entityManager.AddComponentData(entity, new MovementComponent
        {
            velocity = float3.zero,
            acceleration = float3.zero,
            targetPosition = float3.zero,
            lastPosition = float3.zero,
            currentSpeed = 0f,
            maxSpeed = 10f,
            rotationSpeed = 10f,
            hasTarget = false,
            isMoving = false,
            avoidObstacles = true,
            pattern = MovementPattern.Direct,
            patternTimer = 0f
        });
        
        // 태그 추가
        entityManager.AddComponent<DeerTag>(entity);
        entityManager.AddComponent<HerbivoreTag>(entity);
        entityManager.AddComponent<PreyTag>(entity);
        entityManager.AddComponent<FlockMemberTag>(entity);
    }
    
    private void SpawnFood(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            CreateFood(spawnPosition);
        }
    }
    
    private Entity CreateFood(Vector3 position)
    {
        Entity entity = entityManager.CreateEntity();
        
        entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
        entityManager.AddComponentData(entity, new FoodSourceComponent
        {
            foodType = FoodType.Grass,
            nutritionValue = 25f,
            quantity = 100f,
            maxQuantity = 100f,
            regenerationRate = 10f,
            regenerationTimer = 0f,
            isConsumed = false,
            canRegenerate = true
        });
        
        return entity;
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        float x = UnityEngine.Random.Range(-ecosystemSize / 2f, ecosystemSize / 2f);
        float z = UnityEngine.Random.Range(-ecosystemSize / 2f, ecosystemSize / 2f);
        return ecosystemCenter + new Vector3(x, 0, z);
    }
    
    private void UpdateStatistics()
    {
        // 동물 개체수 카운트
        currentStats.rabbitCount = GetAnimalCount<RabbitTag>();
        currentStats.wolfCount = GetAnimalCount<WolfTag>();
        currentStats.deerCount = GetAnimalCount<DeerTag>();
        currentStats.totalAnimals = currentStats.rabbitCount + currentStats.wolfCount + currentStats.deerCount;
        
        // 먹이 개수
        currentStats.foodCount = GetFoodCount();
        
        // 평균 에너지 계산
        currentStats.averageEnergy = CalculateAverageEnergy();
        
        // 생태계 균형 지수 계산
        currentStats.balanceIndex = CalculateBalanceIndex();
    }
    
    private int GetAnimalCount<T>() where T : struct, IComponentData
    {
        var query = entityManager.CreateEntityQuery(typeof(T));
        int count = query.CalculateEntityCount();
        query.Dispose();
        return count;
    }
    
    private int GetFoodCount()
    {
        var query = entityManager.CreateEntityQuery(typeof(FoodSourceComponent));
        int count = query.CalculateEntityCount();
        query.Dispose();
        return count;
    }
    
    private float CalculateAverageEnergy()
    {
        var query = entityManager.CreateEntityQuery(typeof(CreatureComponent));
        var creatures = query.ToComponentDataArray<CreatureComponent>(Allocator.TempJob);
        
        if (creatures.Length == 0)
        {
            creatures.Dispose();
            return 0f;
        }
        
        float totalEnergy = 0f;
        for (int i = 0; i < creatures.Length; i++)
        {
            totalEnergy += creatures[i].energy / creatures[i].maxEnergy;
        }
        
        float average = totalEnergy / creatures.Length;
        creatures.Dispose();
        return average;
    }
    
    private float CalculateBalanceIndex()
    {
        // 이상적인 비율: 토끼 60%, 사슴 30%, 늑대 10%
        if (currentStats.totalAnimals == 0) return 0f;
        
        float rabbitRatio = (float)currentStats.rabbitCount / currentStats.totalAnimals;
        float deerRatio = (float)currentStats.deerCount / currentStats.totalAnimals;
        float wolfRatio = (float)currentStats.wolfCount / currentStats.totalAnimals;
        
        float idealRabbitRatio = 0.6f;
        float idealDeerRatio = 0.3f;
        float idealWolfRatio = 0.1f;
        
        float deviation = Mathf.Abs(rabbitRatio - idealRabbitRatio) +
                         Mathf.Abs(deerRatio - idealDeerRatio) +
                         Mathf.Abs(wolfRatio - idealWolfRatio);
        
        return Mathf.Max(0f, 1f - deviation);
    }
    
    private void UpdateEcosystemBalance()
    {
        // 자동 균형 조정
        if (currentStats.rabbitCount < 5 && currentStats.wolfCount > 0)
        {
            // 토끼가 너무 적으면 추가 스폰
            SpawnAnimals(AnimalType.Rabbit, 10);
            Debug.Log("⚖️ 생태계 균형 조정: 토끼 10마리 추가 스폰");
        }
        
        if (currentStats.wolfCount > currentStats.rabbitCount / 3)
        {
            // 늑대가 너무 많으면 일부 제거
            RemoveExcessPredators();
            Debug.Log("⚖️ 생태계 균형 조정: 과도한 포식자 제거");
        }
        
        if (currentStats.foodCount < currentStats.totalAnimals)
        {
            // 먹이가 부족하면 추가
            SpawnFood(50);
            Debug.Log("🌱 먹이 부족으로 추가 스폰");
        }
    }
    
    private void RemoveExcessPredators()
    {
        var query = entityManager.CreateEntityQuery(typeof(WolfTag), typeof(CreatureComponent));
        var entities = query.ToEntityArray(Allocator.TempJob);
        var creatures = query.ToComponentDataArray<CreatureComponent>(Allocator.TempJob);
        
        // 가장 늙거나 약한 늑대 제거
        for (int i = 0; i < Mathf.Min(2, entities.Length); i++)
        {
            float oldestAge = 0f;
            int oldestIndex = -1;
            
            for (int j = 0; j < creatures.Length; j++)
            {
                if (creatures[j].age > oldestAge)
                {
                    oldestAge = creatures[j].age;
                    oldestIndex = j;
                }
            }
            
            if (oldestIndex >= 0)
            {
                entityManager.DestroyEntity(entities[oldestIndex]);
            }
        }
        
        entities.Dispose();
        creatures.Dispose();
    }
    
    private void DisplayDebugInfo()
    {
        // 화면 상단에 통계 표시 (OnGUI에서 처리)
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("🌍 EcsEchoSystem 통계", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Space(10);
        
        GUILayout.Label($"🐰 토끼: {currentStats.rabbitCount}");
        GUILayout.Label($"🦌 사슴: {currentStats.deerCount}");
        GUILayout.Label($"🐺 늑대: {currentStats.wolfCount}");
        GUILayout.Label($"🌱 먹이: {currentStats.foodCount}");
        GUILayout.Space(5);
        GUILayout.Label($"⚡ 평균 에너지: {currentStats.averageEnergy:F2}");
        GUILayout.Label($"⚖️ 생태계 균형: {currentStats.balanceIndex:F2}");
        
        GUILayout.EndArea();
    }
}

[System.Serializable]
public struct EcosystemStats
{
    public int rabbitCount;
    public int wolfCount;
    public int deerCount;
    public int totalAnimals;
    public int foodCount;
    public float averageEnergy;
    public float balanceIndex;
}