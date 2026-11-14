using System.Numerics;
using Content.Shared._OH;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._OH;

/// <summary>
/// Движок не двигает гриды в предсказании: они стоят на месте до прихода следующего
/// состояния с сервера, а игрок предсказан на несколько тиков вперёд. Продвигаем
/// ближайшие к игроку орбитальные гриды по их скорости на каждом предсказанном тике.
/// </summary>
public sealed class OrbitalGridPredictionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    // Гриды дальше этого расстояния от игрока не продвигаем, чтобы не тратить время
    private const float Range = 100f;

    public override void Update(float frameTime)
    {
        if (!_timing.InPrediction || _player.LocalEntity is not { } player)
            return;

        var playerXform = Transform(player);
        var playerPos = _xform.GetWorldPosition(playerXform);

        var query = EntityQueryEnumerator<OrbitalBodyComponent, MapGridComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var body, out var xform))
        {
            if (body.BodyType != BodyType.Dynamic || body.LinearVelocity == Vector2.Zero)
                continue;

            if (xform.MapID != playerXform.MapID)
                continue;

            // Родитель грида — карта, поэтому локальная позиция равна мировой
            if ((xform.LocalPosition - playerPos).LengthSquared() > Range * Range)
                continue;

            _xform.SetLocalPosition(uid, xform.LocalPosition + body.LinearVelocity * frameTime, xform);
        }
    }
}
