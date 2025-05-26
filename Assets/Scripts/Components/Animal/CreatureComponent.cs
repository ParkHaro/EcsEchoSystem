using Unity.Entities;
using Unity.Mathematics;

public enum AnimalType
{
    Rabbit,
    Wolf,
    Deer,
    Bear,
    Fox
}

public enum Gender
{
    Male,
    Female
}

[System.Serializable]
public struct CreatureComponent : IComponentData
{
    public AnimalType animalType;
    public Gender gender;
    
    // 기본 생명력
    public float energy;
    public float maxEnergy;
    public float hungerRate;
    
    // 생명주기
    public float age;
    public float lifespan;
    public float reproductionAge;
    
    // 물리적 특성
    public float size;
    public float weight;
    public float speed;
    
    // 생식 관련
    public bool isPregnant;
    public float pregnancyTime;
    public float reproductionCooldown;
}