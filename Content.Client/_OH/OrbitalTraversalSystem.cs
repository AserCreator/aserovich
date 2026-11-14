using Content.Shared._OH;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._OH;

/// <summary>
/// На орбитальных картах клиент не меняет родителя локального игрока сам:
/// в предсказании грид не двигается, и проверка "над каким гридом игрок"
/// даёт неверный результат. Родителя меняет сервер, клиент получает его в состоянии.
/// </summary>
public sealed class OrbitalTraversalSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private EntityQuery<OrbitalGravityComponent> _gravQuery;

    public override void Initialize()
    {
        base.Initialize();

        _gravQuery = GetEntityQuery<OrbitalGravityComponent>();
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        var xform = Transform(player);

        // На орбитальной карте автоматическую смену родителя выключаем
        xform.GridTraversal = !(xform.MapUid is { } map && _gravQuery.HasComp(map));
    }
}
