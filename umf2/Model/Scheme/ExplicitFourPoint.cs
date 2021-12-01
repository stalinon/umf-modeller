using AngouriMath.Extensions;
using umf2.Model.Equation;

namespace Model.Scheme
{
    public class ExplicitFourPoint : IScheme
    {
        public string Name { get => "Яная четырехточечная схема"; set { Name = value; } }

        /// <summary>
        ///     Моделирование схемы по параметрам уравнения
        /// </summary>
        public double[,] Model(Equation equation)
        {
            var tau = equation.TimeStep;
            var h = equation.CoordinateStep;
            var a = equation.ParameterA;
            var L = equation.IntegrationLimits.Max - equation.IntegrationLimits.Min;

            var u = equation.InitialConditions.Compile("x");
            var f = equation.RightSideFunction.Compile("x", "t");


            var u_0_t = equation.BorderConditions.X0.Compile("t");
            var u_L_t = equation.BorderConditions.X1.Compile("t");


            var s = (a * tau) / (h * h);

            var y = range(0, L + h, h);
            var t = range(0, 1000 + tau, tau);
            var r = t.Length;
            var c = y.Length;
            var V = new double[r, c + 1];

            for (var i = 0; i < c; i++)
            {
                V[0, i] = u.Substitute(h * i).Real;

            }

            for (var j = 0; j < r - 1; j++)
            {
                V[j, 0] = u_0_t.Substitute(j).Real;
                V[j, c] = u_L_t.Substitute(j).Real;
            }

            for (var i = 0; i < r - 1; i += (int)(equation.TimeStep * 1000))
            {
                for (var j = 1; j < c; j++)
                {
                    if (j == 0) V[i, j] = u_0_t.Substitute(i).Real;
                    if (j == r - 1) V[i, j] = u_L_t.Substitute(i).Real;

                    V[i + 1, j] = V[i, j] + s * (V[i, j - 1] - 2 * V[i, j] + V[i, j + 1]) + tau * f.Substitute(h * (j - 1), tau * (r - 1)).Real;
                }
            }
            return V;
        }

        private double[] range(double a, double b, double n)
        {
            double[] range = new double[(int)((b - a) / n)];
            range[0] = a;
            for (var i = 1; i < range.Length; i++)
                range[i] = range[i - 1] + n;
            return range;
        }
    }
}
