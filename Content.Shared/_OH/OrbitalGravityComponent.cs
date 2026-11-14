using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._OH;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrbitalGravityComponent : Component
{
    /// Гравитационный параметр GM (тайлы³/с²). 20000 при r=200 даёт v=10 м/с, период ≈125 с.
    public double G = 6.67/Math.Pow(10,10);
    [DataField, AutoNetworkedField] public double Mass = 1.23E+14;

    [DataField, AutoNetworkedField] public Vector2 Center = new Vector2(500, 500f);

    /// Ниже этого радиуса сила не растёт (защита от деления на ~0).
    [DataField, AutoNetworkedField] public double MinRadius = 50;
}
