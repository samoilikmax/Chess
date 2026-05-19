using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ChessLogic;

namespace Chess
{
    public partial class MainWindow : Window
    {
        private readonly Image[,] pieceImages = new Image[8, 8];
        private readonly Rectangle[,] highlights = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        private GameState gameState;
        private Position selectedPos = null;

        // ── Часы ────────────────────────────────────────────────────────────
        private DispatcherTimer clock;       // тикает каждую секунду
        private int whiteSeconds;            // оставшееся время белых (сек)
        private int blackSeconds;            // оставшееся время чёрных (сек)
        private int incrementSeconds;        // инкремент после хода (сек)
        private bool clockEnabled;           // false = без лимита

        // ── Сбитые фигуры ───────────────────────────────────────────────────
        private static readonly Dictionary<PieceType, int> startingCount = new()
        {
            { PieceType.Pawn,   8 },
            { PieceType.Rook,   2 },
            { PieceType.Knight, 2 },
            { PieceType.Bishop, 2 },
            { PieceType.Queen,  1 },
            { PieceType.King,   1 },
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
            ShowTimeControlMenu(); // сначала показываем выбор времени
        }

        // ── Инициализация ────────────────────────────────────────────────────

        private void InitializeBoard()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var image = new System.Windows.Controls.Image();
                    pieceImages[r, c] = image;
                    PieceGrid.Children.Add(image);

                    var highlight = new Rectangle();
                    highlights[r, c] = highlight;
                    HighlightGrid.Children.Add(highlight);
                }
        }

        // ── Меню выбора времени ──────────────────────────────────────────────

        private void ShowTimeControlMenu()
        {
            var menu = new TimeControlMenu();
            MenuContainer.Content = menu;

            menu.GameStarted += (totalSeconds, increment) =>
            {
                MenuContainer.Content = null;
                StartGame(totalSeconds, increment);
            };
        }

        // ── Старт партии ─────────────────────────────────────────────────────

        private void StartGame(int totalSeconds, int increment)
        {
            // Настраиваем часы
            clockEnabled    = totalSeconds > 0;
            whiteSeconds    = totalSeconds;
            blackSeconds    = totalSeconds;
            incrementSeconds = increment;

            // Обновляем UI таймеров
            UpdateTimerDisplay();

            // Создаём игровое состояние
            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            UpdateCapturedPieces();

            // Запускаем таймер если нужен лимит
            if (clockEnabled)
            {
                clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                clock.Tick += Clock_Tick;
                clock.Start(); // белые ходят первыми — их таймер идёт
            }
        }

        // ── Тик таймера ──────────────────────────────────────────────────────

        private void Clock_Tick(object sender, EventArgs e)
        {
            if (gameState == null || gameState.IsGameOver())
            {
                clock.Stop();
                return;
            }

            // Уменьшаем время текущего игрока
            if (gameState.CurrentPlayer == Player.White)
            {
                whiteSeconds--;
                if (whiteSeconds <= 0)
                {
                    whiteSeconds = 0;
                    UpdateTimerDisplay();
                    clock.Stop();
                    OnTimeOut(Player.White);
                    return;
                }
            }
            else
            {
                blackSeconds--;
                if (blackSeconds <= 0)
                {
                    blackSeconds = 0;
                    UpdateTimerDisplay();
                    clock.Stop();
                    OnTimeOut(Player.Black);
                    return;
                }
            }

            UpdateTimerDisplay();
        }

        // ── Время вышло ───────────────────────────────────────────────────────

        private void OnTimeOut(Player loser)
        {
            // Устанавливаем результат прямо в GameState через публичное свойство
            // Победитель — противник проигравшего
            gameState.SetResultByTimeout(loser.Opponent());
            ShowGameOver();
        }

        // ── Отображение таймеров ─────────────────────────────────────────────

        private void UpdateTimerDisplay()
        {
            if (!clockEnabled)
            {
                WhiteTimerText.Text = "--:--";
                BlackTimerText.Text = "--:--";
                return;
            }

            WhiteTimerText.Text = FormatTime(whiteSeconds);
            BlackTimerText.Text = FormatTime(blackSeconds);

            // Подсвечиваем красным когда меньше 10 секунд
            WhiteTimerText.Foreground = whiteSeconds <= 10
                ? Brushes.OrangeRed : Brushes.WhiteSmoke;
            BlackTimerText.Foreground = blackSeconds <= 10
                ? Brushes.OrangeRed : Brushes.WhiteSmoke;
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        // ── Добавить инкремент после хода ────────────────────────────────────

        private void ApplyIncrement()
        {
            // Инкремент добавляется игроку который только что походил
            // К моменту вызова CurrentPlayer уже переключился на следующего
            // поэтому добавляем Opponent() — то есть тому кто ходил
            if (!clockEnabled || incrementSeconds == 0) return;

            if (gameState.CurrentPlayer == Player.White)
                blackSeconds += incrementSeconds; // чёрные только что ходили
            else
                whiteSeconds += incrementSeconds; // белые только что ходили

            UpdateTimerDisplay();
        }

        // ── Отрисовка доски ──────────────────────────────────────────────────

        private void DrawBoard(Board board)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    pieceImages[r, c].Source = Images.GetImage(board[r, c]);
        }

        // ── Клики по доске ───────────────────────────────────────────────────

        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen()) return;

            Point point = e.GetPosition(BoardGrid);
            Position pos = ToSquarePosition(point);

            if (selectedPos == null)
                OnFromPositionSelected(pos);
            else
                OnToPositionSelected(pos);
        }

        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);
            return new Position(row, col);
        }

        private void OnFromPositionSelected(Position pos)
        {
            IEnumerable<Move> moves = gameState.LegelMovesForPiece(pos);
            if (moves.Any())
            {
                selectedPos = pos;
                CacheMoves(moves);
                ShowHighlights();
            }
        }

        private void OnToPositionSelected(Position pos)
        {
            selectedPos = null;
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))
            {
                if (move.Type == MoveType.PawnPromotion)
                    HandlePromotion(move.FromPos, move.ToPos);
                else
                    HandleMove(move);
            }
        }

        private void HandlePromotion(Position from, Position to)
        {
            pieceImages[to.Row, to.Column].Source =
                Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            pieceImages[from.Row, from.Column].Source = null;

            // Останавливаем таймер пока игрок выбирает фигуру
            clock?.Stop();

            var promMenu = new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = promMenu;

            promMenu.PieceSelected += type =>
            {
                MenuContainer.Content = null;
                HandleMove(new PawnPromotion(from, to, type));
            };
        }

        private void HandleMove(Move move)
        {
            gameState.MakeMove(move);
            DrawBoard(gameState.Board);
            ApplyIncrement();
            UpdateCapturedPieces();
            UpdateTimerDisplay();

            if (gameState.IsGameOver())
            {
                clock?.Stop();
                ShowGameOver();
            }
            else
            {
                // Перезапускаем таймер (он продолжает — теперь тикает у другого игрока)
                clock?.Start();
            }
        }

        // ── Сбитые фигуры ────────────────────────────────────────────────────

        private void UpdateCapturedPieces()
        {
            CapturedByBlack.Children.Clear();
            CapturedByWhite.Children.Clear();

            var whiteOnBoard = new Dictionary<PieceType, int>();
            var blackOnBoard = new Dictionary<PieceType, int>();

            foreach (PieceType type in startingCount.Keys)
            {
                whiteOnBoard[type] = 0;
                blackOnBoard[type] = 0;
            }

            foreach (Position pos in gameState.Board.PiecePositions())
            {
                Piece piece = gameState.Board[pos];
                if (piece.Color == Player.White)
                    whiteOnBoard[piece.Type]++;
                else
                    blackOnBoard[piece.Type]++;
            }

            foreach (PieceType type in startingCount.Keys)
            {
                int captured = startingCount[type] - whiteOnBoard[type];
                for (int i = 0; i < captured; i++)
                    CapturedByBlack.Children.Add(CreateCapturedIcon(Player.White, type));
            }

            foreach (PieceType type in startingCount.Keys)
            {
                int captured = startingCount[type] - blackOnBoard[type];
                for (int i = 0; i < captured; i++)
                    CapturedByWhite.Children.Add(CreateCapturedIcon(Player.Black, type));
            }
        }

        private static System.Windows.Controls.Image CreateCapturedIcon(Player color, PieceType type)
        {
            return new System.Windows.Controls.Image
            {
                Source  = Images.GetImage(color, type),
                Width   = 32,
                Height  = 32,
                Margin  = new Thickness(2),
                Opacity = 0.85
            };
        }

        // ── Подсветка ходов ───────────────────────────────────────────────────

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();
            foreach (Move move in moves)
                moveCache[move.ToPos] = move;
        }

        private void ShowHighlights()
        {
            Color color = Color.FromArgb(150, 125, 255, 125);
            foreach (Position to in moveCache.Keys)
                highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
        }

        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
                highlights[to.Row, to.Column].Fill = Brushes.Transparent;
        }

        private bool IsMenuOnScreen() => MenuContainer.Content != null;

        // ── Меню конца игры ───────────────────────────────────────────────────

        private void ShowGameOver()
        {
            var gameOverMenu = new GameOverMenu(gameState);
            MenuContainer.Content = gameOverMenu;

            gameOverMenu.OptionSelected += option =>
            {
                if (option == Option.Restart)
                {
                    MenuContainer.Content = null;
                    RestartGame();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
        }

        // ── Рестарт ───────────────────────────────────────────────────────────

        private void RestartGame()
        {
            clock?.Stop();
            clock = null;
            selectedPos = null;
            HideHighlights();
            moveCache.Clear();

            // Снова показываем выбор времени
            ShowTimeControlMenu();
        }

        // ── Пауза (Escape) ────────────────────────────────────────────────────

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsMenuOnScreen() && e.Key == Key.Escape)
                ShowPauseMenu();
        }

        private void ShowPauseMenu()
        {
            clock?.Stop(); // останавливаем таймер на паузе

            var pauseMenu = new PauseMenu();
            MenuContainer.Content = pauseMenu;

            pauseMenu.OptionSelected += option =>
            {
                MenuContainer.Content = null;

                if (option == Option.Restart)
                {
                    RestartGame();
                }
                else
                {
                    // Continue — возобновляем таймер
                    clock?.Start();
                }
            };
        }
    }
}