using System;
using System.Windows;
using System.Windows.Controls;

namespace Chess
{
    /// <summary>
    /// Interaction logic for TimeControlMenu.xaml
    /// </summary>
    public partial class TimeControlMenu : UserControl
    {
        // Событие срабатывает когда игрок нажал Start
        // Передаёт: общее время в секундах (0 = без лимита) и инкремент в секундах
        public event Action<int, int> GameStarted;

        public TimeControlMenu()
        {
            InitializeComponent();
        }

        // Клик на пресет — заполняет поля и сразу стартует
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string[] parts = btn.Tag.ToString().Split(',');

            int totalSeconds = int.Parse(parts[0]);
            int increment = int.Parse(parts[1]);

            GameStarted?.Invoke(totalSeconds, increment);
        }

        // Клик на Start — читает кастомные поля
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            // Парсим минуты
            if (!int.TryParse(MinutesBox.Text, out int minutes) || minutes < 0)
            {
                MinutesBox.Text = "5";
                minutes = 5;
            }

            // Парсим инкремент
            if (!int.TryParse(IncrementBox.Text, out int increment) || increment < 0)
            {
                IncrementBox.Text = "0";
                increment = 0;
            }

            int totalSeconds = minutes * 60;
            GameStarted?.Invoke(totalSeconds, increment);
        }
    }
}