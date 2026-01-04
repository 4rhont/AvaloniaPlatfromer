using Avalonia.Controls;
using GameApp.ViewModels;

namespace GameApp.Views
{
    public partial class WeatherLayer : UserControl
    {
        public WeatherLayer()
        {
            InitializeComponent();

            this.DataContextChanged += (_, __) =>
            {
                if (this.DataContext is WeatherViewModel vm)
                {
                    vm.SetCanvas(WeatherCanvas);
                }
            };
        }
    }
}