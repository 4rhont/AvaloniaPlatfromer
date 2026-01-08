using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameApp.Core.Services;
using GameApp.Core.ViewModels;
using GameApp.ViewModels;
using GameApp.Views;
using System;

namespace GameApp.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Инициализируем снег
            var snowCanvas = this.FindControl<Canvas>("SnowCanvas");
            if (snowCanvas != null && _viewModel != null)
            {
                _viewModel.SetSnowCanvas(snowCanvas);
            }

            UpdateLoadButtonState();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel?.StopSnowAnimation();
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
            StartGame("level1", isNewGame: true);
        }

        private void LoadGame_Click(object? sender, RoutedEventArgs e)
        {
            var saveData = SaveSystemService.LoadGame();
            if (saveData == null)
                return;

            StartGame(saveData.CurrentLevelId, isNewGame: false, playerHealth: saveData.PlayerHealth);
        }

        private void StartGame(string levelId, bool isNewGame, int playerHealth = 5)
        {
            var debugCheckBox = this.FindControl<CheckBox>("DebugModeCheckBox");
            bool debugMode = debugCheckBox?.IsChecked ?? false;

            if (isNewGame)
                SaveSystemService.DeleteSave();

            var gameViewModel = new GameViewModel(debugMode: debugMode);
            gameViewModel.LoadLevel(levelId);
            gameViewModel.Player.CurrentHealth = playerHealth;

            var gameView = new GameView(gameViewModel);
            gameView.Show();

            _viewModel?.StopSnowAnimation();
            this.Close();
        }
    }
}
