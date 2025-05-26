using Unity.Entities;

public enum FoodType
{
    Grass,
    Berries,
    Nuts,
    Meat,
    Fish,
    Insects
}

[System.Serializable]
public struct FoodSourceComponent : IComponentData
{
    public FoodType foodType;
    public float nutritionValue;
    public float quantity;
    public float maxQuantity;
    public float regenerationRate;
    public float regenerationTimer;
    public bool isConsumed;
    public bool canRegenerate;
}