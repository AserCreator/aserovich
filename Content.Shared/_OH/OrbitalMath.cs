namespace Content.Shared._OH;

public static class OrbitalMath
{
    public static bool TryGetOrbit(DoubleVec r, DoubleVec v, double mu, out OrbitData orbit)
    {
        orbit = default;

        var rLen = r.Length();
        if (rLen < 1e-6)
            return false;

        // Удельный угловой момент (скаляр, т.к. движение в плоскости).
        var h = r.X * v.Y - r.Y * v.X;
        if (Math.Abs(h) < 1e-6) // чисто радиальное падение, эллипса нет
            return false;

        // Вектор эксцентриситета.
        var v2 = v.X * v.X + v.Y * v.Y;
        var rv = r.X * v.X + r.Y * v.Y;
        var k = v2 - mu / rLen;
        var ex = (k * r.X - rv * v.X) / mu;
        var ey = (k * r.Y - rv * v.Y) / mu;

        var e = Math.Sqrt(ex * ex + ey * ey);
        var omega = e > 1e-9 ? Math.Atan2(ey, ex) : 0;

        // Знак h задаёт направление обращения.
        var dir = h > 0 ? 1 : -1;

        orbit = new OrbitData(mu, h * h / mu, e, omega, dir);
        return true;
    }

    /// Истинная аномалия -> средняя аномалия.
    public static double TrueToMean(in OrbitData o, double nu)
    {
        if (o.E < 1)
        {
            // Эллипс: E — эксцентрическая аномалия.
            var ecc = 2 * Math.Atan2(
                Math.Sqrt(1 - o.E) * Math.Sin(nu / 2),
                Math.Sqrt(1 + o.E) * Math.Cos(nu / 2));
            return ecc - o.E * Math.Sin(ecc);
        }

        // Гипербола: F — гиперболическая аномалия.
        var f = 2 * Math.Atanh(Math.Sqrt((o.E - 1) / (o.E + 1)) * Math.Tan(nu / 2));
        return o.E * Math.Sinh(f) - f;
    }

    /// Средняя аномалия -> истинная аномалия (решение уравнения Кеплера методом Ньютона).
    public static double MeanToTrue(in OrbitData o, double m)
    {
        if (o.E < 1)
        {
            m = Math.IEEERemainder(m, 2 * Math.PI);

            // Начальное приближение Данби — сходится и при больших e.
            var ecc = m + 0.85 * o.E * Math.Sign(Math.Sin(m));
            for (var i = 0; i < 30; i++)
            {
                var d = (ecc - o.E * Math.Sin(ecc) - m) / (1 - o.E * Math.Cos(ecc));
                ecc -= d;
                if (Math.Abs(d) < 1e-12)
                    break;
            }

            return 2 * Math.Atan2(
                Math.Sqrt(1 + o.E) * Math.Sin(ecc / 2),
                Math.Sqrt(1 - o.E) * Math.Cos(ecc / 2));
        }

        // M = e*sinh(F) - F
        var f = Math.Asinh(m / o.E);
        for (var i = 0; i < 50; i++)
        {
            var d = (o.E * Math.Sinh(f) - f - m) / (o.E * Math.Cosh(f) - 1);
            f -= d;
            if (Math.Abs(d) < 1e-12)
                break;
        }

        return 2 * Math.Atan(Math.Sqrt((o.E + 1) / (o.E - 1)) * Math.Tanh(f / 2));
    }
}
