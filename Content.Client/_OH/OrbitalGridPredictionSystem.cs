using System.Numerics;
using Content.Shared._OH;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Controllers;

namespace Content.Client._OH;

/// <summary>
/// Движок не двигает гриды в предсказании: они стоят на месте до прихода следующего
/// состояния с сервера, а игрок предсказан на несколько тиков вперёд. Продвигаем
/// ближайшие к игроку орбитальные гриды по их скорости на каждом предсказанном тике.
/// </summary>
public sealed class OrbitalGridPredictionSystem : VirtualController
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    // Гриды дальше этого расстояния от игрока не продвигаем
    private const float Margin = 15f;
    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedMoverController));
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        if (!prediction || _player.LocalEntity is not { } player)
            return;

        var playerXform = Transform(player);
        var playerPos = _xform.GetWorldPosition(playerXform);
        var query = EntityQueryEnumerator<OrbitalBodyComponent, MapGridComponent, PhysicsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var grid, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic || body.LinearVelocity == Vector2.Zero)
                continue;

            if (xform.MapID != playerXform.MapID)
                continue;

            // Положение игрока в системе координат грида (учитывает поворот грида)
            var localPos = Vector2.Transform(playerPos, _xform.GetInvWorldMatrix(xform));

            if (!grid.LocalAABB.Enlarged(Margin).Contains(localPos))
                continue;

            // Родитель грида — карта, поэтому локальная позиция равна мировой
            _xform.SetLocalPosition(uid, xform.LocalPosition + body.LinearVelocity * frameTime, xform);
    }
    }
}
