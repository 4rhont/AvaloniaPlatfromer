using Avalonia.Controls;
using Avalonia.Interactivity;
using GameApp.Core.ViewModels;
using GameApp.ViewModels;

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
            bool debugMode = DebugCheckBox.IsChecked ?? false;
            var gameVM = new GameViewModel(debugMode);
            var gameView = new GameView(gameVM);
            gameView.Closed += (s, args) =>
            {
                // gameVM.Dispose();  

            };
            gameView.Show();
        }
    }
}