using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GameApp.Core.ViewModels;
using GameApp.Core.Input;
using System;

namespace GameApp.Views
{
    public partial class GameView : Window
    {
        public GameView()
        {
            InitializeComponent();

            Focusable = true;
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            Activated += OnActivated;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = DataContext as GameViewModel;
            if (viewModel == null) return;

            var action = ConvertKeyToAction(e.Key, true);
            if (action.HasValue)
            {
                viewModel.StartAction(action.Value);
                e.Handled = true;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            var viewModel = DataContext as GameViewModel;
            if (viewModel == null) return;

            var action = ConvertKeyToAction(e.Key, false);
            if (action.HasValue)
            {
                viewModel.StopAction(action.Value);
                e.Handled = true;
            }
        }

        private GameAction? ConvertKeyToAction(Key key, bool isKeyDown)
        {
            switch (key)
            {
                case Key.Left:
                case Key.A:
                    return GameAction.MoveLeft;
                case Key.Right:
                case Key.D:
                    return GameAction.MoveRight;
                case Key.Up:
                case Key.W:
                case Key.Space:
                    return GameAction.Jump; // Всегда возвращаем Jump для этих клавиш
                default:
                    return null;
            }
        }
    }
}