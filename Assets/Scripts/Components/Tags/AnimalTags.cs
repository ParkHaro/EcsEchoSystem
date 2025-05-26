using Unity.Entities;

// 동물 종류별 태그
public struct RabbitTag : IComponentData { }
public struct WolfTag : IComponentData { }
public struct DeerTag : IComponentData { }
public struct BearTag : IComponentData { }
public struct FoxTag : IComponentData { }

// 행동 분류 태그
public struct HerbivoreTag : IComponentData { }
public struct CarnivoreTag : IComponentData { }
public struct OmnivoreTag : IComponentData { }

// 먹이사슬 태그
public struct PredatorTag : IComponentData { }
public struct PreyTag : IComponentData { }

// 상태 태그
public struct HungryTag : IComponentData { }
public struct ThirstyTag : IComponentData { }
public struct TiredTag : IComponentData { }
public struct PregnantTag : IComponentData { }
public struct YoungTag : IComponentData { }
public struct OldTag : IComponentData { }

// 특수 행동 태그
public struct AlphaTag : IComponentData { }  // 무리 리더
public struct FlockMemberTag : IComponentData { }
public struct TerritorialTag : IComponentData { }