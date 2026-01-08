using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameApp.Core.Services;
using GameApp.Core.ViewModels;
using GameApp.Views;

namespace GameApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            UpdateLoadButtonState();
        }

        private void UpdateLoadButtonState()
        {
            var hasSave = SaveSystemService.HasSave();
            var loadButton = this.FindControl<Button>("LoadGameButton");
            var saveInfoBorder = this.FindControl<Border>("SaveInfoBorder");

            if (loadButton != null)
                loadButton.IsEnabled = hasSave;

            if (saveInfoBorder != null)
                saveInfoBorder.IsVisible = hasSave;

            if (hasSave)
            {
                ShowSaveInfo();
            }
        }

        private void ShowSaveInfo()
        {
            var data = SaveSystemService.LoadGame();
            if (data == null)
                return;

            var saveTimeText = this.FindControl<TextBlock>("SaveTimeText");
            var saveLevelText = this.FindControl<TextBlock>("SaveLevelText");

            if (saveTimeText != null)
                saveTimeText.Text = $"Date: {data.SaveTime:yyyy-MM-dd HH:mm}";

            if (saveLevelText != null)
                saveLevelText.Text = $"Level: {data.CurrentLevelId} | HP: {data.PlayerHealth}/5";
        }

        private void NewGame_Click(object? sender, RoutedEventArgs e)
        {
            SaveSystemService.DeleteSave();

            var gameViewModel = new GameViewModel(debugMode: false);
            gameViewModel.LoadLevel("level1");
            gameViewModel.Player.CurrentHealth = 5;

            // КЛЮЧ: Открываем GameView как новое ОКНО
            var gameView = new GameView(gameViewModel);
            gameView.Show();

            // Закрываем меню
            this.Close();
        }

        private void LoadGame_Click(object? sender, RoutedEventArgs e)
        {
            var saveData = SaveSystemService.LoadGame();
            if (saveData == null)
                return;

            var gameViewModel = new GameViewModel(debugMode: false);
            gameViewModel.LoadLevel(saveData.CurrentLevelId);
            gameViewModel.Player.CurrentHealth = saveData.PlayerHealth;

            // КЛЮЧ: Открываем GameView как новое ОКНО
            var gameView = new GameView(gameViewModel);
            gameView.Show();

            // Закрываем меню
            this.Close();
        }
    }
}
