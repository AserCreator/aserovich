using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._OH;

// ВРЕМЕННО, для отладки. Удалить после поиска причины.
public sealed class OrbitalPredictDebugSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityUid _lastParent;
    private int _logTicks;

    public override void Initialize()
    {
        base.Initialize();
        Log.Warning("OrbitalPredictDebugSystem запущена");
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        var xform = Transform(player);
        var parent = xform.ParentUid;

        // Логируем 10 тиков после каждой смены родителя
        if (parent != _lastParent)
        {
            _lastParent = parent;
            _logTicks = 10;
        }

        if (_logTicks <= 0)
            return;

        _logTicks--;

        var side = _timing.IsFirstTimePredicted ? "pred" : "resim";
        Log.Warning($"tick={_timing.CurTick} {side} parent={parent} " +
                    $"playerPos={_xform.GetWorldPosition(player)} " +
                    $"parentPos={_xform.GetWorldPosition(parent)} " +
                    $"parentVel={_physics.GetMapLinearVelocity(parent)}");
    }
}
