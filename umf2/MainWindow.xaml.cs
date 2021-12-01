using Model.Scheme;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ImageMagick;
using umf2.Model.Equation;
using System.Collections.Generic;

namespace umf2
{
    public partial class MainWindow : Window
    {
        static Equation equation;
        static CancellationTokenSource cts = new CancellationTokenSource();
        public MainWindow()
        {
            InitializeComponent();
            EquationType.ItemsSource = new List<string>
            {
                "Уравнение теплопроводности",
                //"Уравнение переноса",
                //"Волновое уравнение"
            };
            Application.Current.Exit += Current_Exit;
        }
        /// <summary>
        ///     Создание гиф-анимации из картинок .png
        ///     в временной папке Resourses/tmp
        /// </summary>
        private void GifMaker_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "GIF animation(*.gif)| *.gif";
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog() == true)
                Task.Factory.StartNew(() =>
                {
                    using (var collection = new MagickImageCollection())
                    {
                        var directoryPath = $"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}/Resourses/tmp/";
                        var directoryInfo = new DirectoryInfo(directoryPath);
                        var files = directoryInfo.GetFiles();

                        for (var i = 0; i < files.Length; i++)
                        {
                            collection.Add(directoryPath + $"{i}.png");
                            collection[i].AnimationDelay = 5;
                        }

                        collection.Write($"{saveFileDialog.FileName}");
                        DeleteTMP();
                    }
                });
        }

        /// <summary>
        ///     Моделирование и отображение процесса
        /// </summary>
        private void Model_Click(object sender, RoutedEventArgs e)
        {
            if (equation == null)
            {
                MessageBox.Show("Параметры уравнения не указаны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var scheme = new ExplicitFourPoint();
            double[,] array;

            Task.Factory.StartNew(() =>
            {
                var task = new Task<double[,]>(() => scheme.Model(equation));
                task.Start();
                try
                {
                    while (!task.IsCompleted)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Progress.Value += Progress.Maximum / 100;
                            Thread.Sleep(100);
                        });
                    }
                }
                catch (TaskCanceledException ex)
                {
                    ex.Task.Dispose();
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress.Value = Progress.Maximum;
                    Thread.Sleep(1000);
                    Progress.Value = 0;
                });

                array = task.Result;

                var plotOperation = new Task(() => Plotter(array, cts.Token), cts.Token);
                plotOperation.Start();
            });
        }

        /// <summary>
        ///     Действия перед закрытием программы
        /// </summary>
        private void Current_Exit(object sender, ExitEventArgs e)
        {
            cts.Cancel();
            DeleteTMP();
        }

        /// <summary>
        ///     Изменение типа уравнения при выборе нового в комбобоксе
        /// </summary>
        private void EquationType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EquationType.SelectedItem.ToString() == "Уравнение теплопроводности")
            {
                equation = new HeatEquation();
                SchemeType.ItemsSource = equation.AvailableSchemes.Select(x => x.Name);

            }
            else if (EquationType.SelectedItem.ToString() == "Уравнение переноса")
            {

            }
            else if (EquationType.SelectedItem.ToString() == "Волновое уравнение")
            {

            }
        }

        /// <summary>
        ///     Сохранение введенных параметров
        /// </summary>
        private void SaveParameters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                equation.BorderConditions = (BorderConditions1.Text, BorderConditions2.Text);
                equation.CoordinateStep = double.Parse(CoordinateStep.Text);
                equation.InitialConditions = InitialConditions.Text;
                equation.IntegrationLimits = (double.Parse(IntegrationLimits1.Text), double.Parse(IntegrationLimits2.Text));
                equation.MeasurementLimits = (double.Parse(MeasurementLimits1.Text), double.Parse(MeasurementLimits2.Text));
                equation.ParameterA = double.Parse(ParameterA.Text);
                equation.RightSideFunction = RightSideFunction.Text;
                equation.TimeStep = double.Parse(TimeStep.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        ///     Удалить временную папку
        /// </summary>
        private static void DeleteTMP()
        {
            var directoryPath = $"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}/Resourses/tmp/";
            var directoryInfo = new DirectoryInfo(directoryPath);
            if (directoryInfo.Exists)
            {
                foreach(var file in directoryInfo.GetFiles())
                    file.Delete();
                directoryInfo.Delete();
            }
        }

        /// <summary>
        ///     Построение графика по массиву
        /// </summary>
        private void Plotter(double[,] arrayYT, CancellationToken token)
        {
            DeleteTMP();
            var directoryPath = $"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}/Resourses/tmp/";
            var directoryInfo = new DirectoryInfo(directoryPath);
            directoryInfo.Create();
            var a = equation.IntegrationLimits.Min;
            var b = equation.IntegrationLimits.Max;
            var length = arrayYT.GetLength(0);
            for (var index = 0; index < length; index++)
            {
                var arrayY = GetRow(arrayYT, index);
                if (arrayY != null)
                {
                    var n = arrayY.Length;
                    var xPoints = SegmentPointsGenerator(a, (b - a) / n, b);
                    if (!arrayY.Contains(double.NaN) && !token.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Chart.Reset();
                            Chart.Plot.SetAxisLimitsX(equation.IntegrationLimits.Min, equation.IntegrationLimits.Max);
                            Chart.Plot.SetAxisLimitsY(equation.MeasurementLimits.Min, equation.MeasurementLimits.Max);
                            Chart.Plot.AddScatter(xPoints, arrayY);
                            Chart.Refresh();
                            if (index%5 == 0)
                                Chart.Plot.SaveFig($"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))}/Resourses/tmp/{index/5}.png");

                            Progress.Value += (double)(Progress.Maximum / length)*2000;
                        });
                        Thread.Sleep(25);
                    }
                }
            }
        }

        /// <summary>
        ///     Генерация n точек от а до b
        /// </summary>
        private static double[] SegmentPointsGenerator(double a, double n, double b)
        {
            double[] segment = new double[(int)((b - a) / n)];
            segment[0] = a;
            for (var i = 1; i < segment.Length; i++)
                segment[i] = segment[i - 1] + n;
            return segment;
        }

        /// <summary>
        ///     Получить строку двумерного массива
        /// </summary>
        private static T[] GetRow<T>(T[,] matrix, int row)
        {
            if (matrix.GetLength(0) <= row)
                return null;

            T[] row_res = new T[matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                row_res[i] = matrix[row, i];
            }

            return row_res;
        }

    }
}
