using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessLogic;

namespace Chess
{
    public static class Images
    {
        private static readonly Dictionary<PieceType, ImageSource> whiteSources = new() 
        {
            {PieceType.Pawn, LoadImage("Icons/PawnW.png") },
            {PieceType.Bishop, LoadImage("Icons/BishopW.png") },
            {PieceType.Rook, LoadImage("Icons/RookW.png") },
            {PieceType.King, LoadImage("Icons/KingW.png") },
            {PieceType.Queen, LoadImage("Icons/QueenW.png") },
            {PieceType.Knight, LoadImage("Icons/KnightW.png") },
        };

        private static readonly Dictionary<PieceType, ImageSource> blackSources = new()
        {
            {PieceType.Pawn, LoadImage("Icons/PawnB.png") },
            {PieceType.Bishop, LoadImage("Icons/BishopB.png") },
            {PieceType.Rook, LoadImage("Icons/RookB.png") },
            {PieceType.King, LoadImage("Icons/KingB.png") },
            {PieceType.Queen, LoadImage("Icons/QueenB.png") },
            {PieceType.Knight, LoadImage("Icons/KnightB.png") },
        };

        //Метод для загрузки картинок из файла
        private static ImageSource LoadImage(string filePath) 
        {
            return new BitmapImage(new Uri(filePath, UriKind.Relative));
        }

        //Метод для получения типа фигуры
        public static ImageSource GetImage(Player color, PieceType type)
        {
            return color switch
            {
                Player.White => whiteSources[type],
                Player.Black => blackSources[type],
                _ => null
            };
        }

        public static ImageSource GetImage(Piece piece)
        {
            if(piece == null) 
            {
                return null;
            }

            return GetImage(piece.Color, piece.Type);
        }
    }
}
