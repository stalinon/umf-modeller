using Model.Scheme;
using System.Collections.Generic;
using System.Linq;

namespace umf2.Model.Equation
{
    /// <summary>
    ///     Интерфейс для уравнений
    /// </summary>
    public abstract class Equation
    {
        /// <summary>
        ///     Начальные условия  
        /// </summary>
        public string InitialConditions { get; set; }

        /// <summary>
        ///     Граничные условия
        /// </summary>
        public (string X0, string X1) BorderConditions { get; set; }

        /// <summary>
        ///     Функция в правой части       
        /// </summary>
        public string RightSideFunction { get; set; }

        /// <summary>
        ///     Значение параметра а
        /// </summary>
        public double ParameterA { get; set; }

        /// <summary>
        ///     Шаг по координате
        /// </summary>
        public double CoordinateStep { get; set; }

        /// <summary>
        ///     Шаг по времени
        /// </summary>
        public double TimeStep { get; set; }

        /// <summary>
        ///     Границы интегрирования
        /// </summary>
        public (double Min, double Max) IntegrationLimits { get; set; }

        /// <summary>
        ///     Границы измерения
        /// </summary>
        public (double Min, double Max) MeasurementLimits { get; set; }

        /// <summary>
        ///     Список доступных схем
        /// </summary>
        public List<IScheme> AvailableSchemes { get => new List<IScheme> { new ExplicitFourPoint(), new Implicit() }; }

        public double[,] Model(string schemeName)
        {
            if (AvailableSchemes.Select(x => x.Name).Contains(schemeName))
            {
                return AvailableSchemes.Where(x => x.Name == schemeName).Single().Model(this);
            }
            return null;
        }
    }
}
