
using umf2.Model.Equation;

namespace Model.Scheme
{
    public interface IScheme
    {
        /// <summary>
        ///     Название схемы
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     Моделирование схемы по параметрам уравнения
        /// </summary>
        double[,] Model(Equation equation);
    }
}