using Avalonia.Controls;
using Avalonia.Interactivity;
using GameApp.Core.ViewModels;

namespace GameApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            var gameView = new GameView
            {
                DataContext = new GameViewModel()
            };
            gameView.Closed += (s, args) =>
            {
                // Освобождаем ресурсы при закрытии окна
                if (gameView.DataContext is GameViewModel viewModel)
                    viewModel.Dispose();
            };
            gameView.Show();
            this.Close();
        }
    }
}