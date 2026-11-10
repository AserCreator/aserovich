namespace Content.Shared._OH;

/// Элементы орбиты в плоскости. Direction: +1 против часовой, -1 по часовой.
public readonly record struct OrbitData(double Mu, double P, double E, double Omega, int Direction = 1)
{
    public bool IsBound => E < 1;
    public double Periapsis => P / (1 + E);
    public double Apoapsis => IsBound ? P / (1 - E) : double.PositiveInfinity;

    /// Модуль большой полуоси (для гиперболы берётся |a|).
    public double SemiMajorAxis => Math.Abs(P / (1 - E * E));

    /// Среднее движение n = sqrt(mu / a^3), рад/с.
    public double MeanMotion
    {
        get
        {
            var a = SemiMajorAxis;
            return Math.Sqrt(Mu / (a * a * a));
        }
    }

    /// Положение относительно центра при истинной аномалии nu.
    public DoubleVec PositionAt(double nu)
    {
        var r = P / (1 + E * Math.Cos(nu));
        var a = Omega + Direction * nu;
        return new DoubleVec(r * Math.Cos(a), r * Math.Sin(a));
    }

    /// Скорость при истинной аномалии nu.
    public DoubleVec VelocityAt(double nu)
    {
        var k = Math.Sqrt(Mu / P);
        var vr = k * E * Math.Sin(nu);      // радиальная составляющая
        var vt = k * (1 + E * Math.Cos(nu)); // тангенциальная составляющая

        var a = Omega + Direction * nu;
        var cos = Math.Cos(a);
        var sin = Math.Sin(a);

        // Единичный радиальный вектор (cos, sin), касательный: (-sin, cos) * Direction.
        return new DoubleVec(
            vr * cos - Direction * vt * sin,
            vr * sin + Direction * vt * cos);
    }

    /// Истинная аномалия точки r, лежащей на этой орбите.
    public double TrueAnomalyOf(DoubleVec r)
    {
        var angle = Math.Atan2(r.Y, r.X);
        return Math.IEEERemainder(Direction * (angle - Omega), 2 * Math.PI);
    }
}
