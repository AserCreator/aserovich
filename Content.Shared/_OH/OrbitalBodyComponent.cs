using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared._OH;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrbitalBodyComponent : Component
{
    /// Выдать круговую скорость при MapInit.
    [DataField] public bool InitCircular = true;

    /// Ускорение (тайл/с²) сверх гравитации, начиная с которого считаем, что на грид
    /// действует внешняя сила (двигатель, столкновение). Должно быть меньше самой слабой тяги.
    [DataField] public double ThrustThreshold = 0.02;

    /// Сколько тиков подряд без внешних сил нужно, чтобы вернуться на рельсы.
    [DataField] public int SettleTicks = 60;

    // --- Состояние рельсов. Не DataField: после рестарта/загрузки карты Epoch бессмыслен. ---

    /// Тело двигается по аналитической орбите, физика гравитацию не считает.
    [ViewVariables, AutoNetworkedField] public bool OnRails;

    [ViewVariables, AutoNetworkedField] public double Mu;
    [ViewVariables, AutoNetworkedField] public double P;
    [ViewVariables, AutoNetworkedField] public double Ecc;
    [ViewVariables, AutoNetworkedField] public double Omega;
    [ViewVariables, AutoNetworkedField] public int Dir = 1;

    /// Средняя аномалия в момент Epoch.
    [ViewVariables, AutoNetworkedField] public double M0;
    [ViewVariables, AutoNetworkedField] public TimeSpan Epoch;

    // --- Рантайм, не синхронизируется. ---

    /// Скорость, которую мы выставили на рельсах в прошлом тике.
    public Vector2 ExpectedVel;

    /// Скорость и ускорение гравитации прошлого тика в физическом режиме.
    public Vector2 PrevVel;
    public Vector2 PrevAccel;

    /// Сколько тиков подряд невязка ускорения ниже порога.
    public int Settled;
}
