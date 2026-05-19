using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChessLogic;

namespace Chess
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Image[,] pieceImages = new Image[8, 8];
        private readonly Rectangle[,] highlights = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        private GameState gameState;
        private Position selectedPos = null;

        // Начальный состав фигур для подсчёта сбитых
        // ключ = тип фигуры, значение = сколько их было в начале
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

            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            UpdateCapturedPieces();
        }

        // Создает 64 пустые клетки
        private void InitializeBoard()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    Image image = new Image();
                    pieceImages[r, c] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    highlights[r, c] = highlight;
                    HighlightGrid.Children.Add(highlight);
                }
        }

        // Метод для отрисовки начального положения фигур
        private void DrawBoard(Board board)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = board[r, c];
                    pieceImages[r, c].Source = Images.GetImage(piece);
                }
        }

        // Подсчитывает сбитые фигуры и обновляет боковые панели
        private void UpdateCapturedPieces()
        {
            CapturedByBlack.Children.Clear();
            CapturedByWhite.Children.Clear();

            // Считаем сколько фигур каждого цвета осталось на доске
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

            // Сравниваем с начальным составом — разница и есть сбитые
            // Белые фигуры съеденные чёрными — показываем слева (CapturedByBlack)
            foreach (PieceType type in startingCount.Keys)
            {
                int captured = startingCount[type] - whiteOnBoard[type];
                for (int i = 0; i < captured; i++)
                    CapturedByBlack.Children.Add(CreateCapturedIcon(Player.White, type));
            }

            // Чёрные фигуры съеденные белыми — показываем справа (CapturedByWhite)
            foreach (PieceType type in startingCount.Keys)
            {
                int captured = startingCount[type] - blackOnBoard[type];
                for (int i = 0; i < captured; i++)
                    CapturedByWhite.Children.Add(CreateCapturedIcon(Player.Black, type));
            }
        }

        // Создаёт маленькую иконку сбитой фигуры
        private static Image CreateCapturedIcon(Player color, PieceType type)
        {
            return new Image
            {
                Source = Images.GetImage(color, type),
                Width = 36,
                Height = 36,
                Margin = new Thickness(2),
                Opacity = 0.85
            };
        }

        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen())
                return;

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

        // Ставит игру на паузу и показывает меню превращения пешки
        private void HandlePromotion(Position from, Position to)
        {
            pieceImages[to.Row, to.Column].Source = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            pieceImages[from.Row, from.Column].Source = null;

            PromotionMenu promMenu = new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = promMenu;

            promMenu.PieceSelected += type =>
            {
                MenuContainer.Content = null;
                Move promMove = new PawnPromotion(from, to, type);
                HandleMove(promMove);
            };
        }

        private void HandleMove(Move move)
        {
            gameState.MakeMove(move);
            DrawBoard(gameState.Board);
            UpdateCapturedPieces(); // обновляем панели после каждого хода

            if (gameState.IsGameOver())
                ShowGameOver();
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();
            foreach (Move move in moves)
                moveCache[move.ToPos] = move;
        }

        // Подсвечивает зеленым
        private void ShowHighlights()
        {
            Color color = Color.FromArgb(150, 125, 255, 125);
            foreach (Position to in moveCache.Keys)
                highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
        }

        // Убирает подсветку
        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
                highlights[to.Row, to.Column].Fill = Brushes.Transparent;
        }

        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }

        // Выбор команды в меню конца игры
        private void ShowGameOver()
        {
            GameOverMenu gameOverMenu = new GameOverMenu(gameState);
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

        // Перезапуск игры
        private void RestartGame()
        {
            selectedPos = null;
            HideHighlights();
            moveCache.Clear();
            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            UpdateCapturedPieces(); // сбрасываем панели
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsMenuOnScreen() && e.Key == Key.Escape)
                ShowPauseMenu();
        }

        private void ShowPauseMenu()
        {
            PauseMenu pauseMenu = new PauseMenu();
            MenuContainer.Content = pauseMenu;

            pauseMenu.OptionSelected += option =>
            {
                MenuContainer.Content = null;
                if (option == Option.Restart)
                    RestartGame();
            };
        }
    }
}