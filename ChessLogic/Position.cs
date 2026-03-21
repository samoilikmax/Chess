using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessLogic
{
    public class Position
    {
        //Верхний левый квадрат [0,0]
        public int Row { get; }
        public int Column { get; }

        public Position(int row, int column) {
            Row = row; 
            Column = column; 
        }

        //Для цветов используется Player
        public Player SquareColor()
        {
            return (Row + Column) % 2 == 0 ? Player.White : Player.Black;
        }

        //Переопределение Equals(сравнение по значению а не ссылке) и GetHashCode (ctrl+.)
        public override bool Equals(object obj)
        {
            return obj is Position position &&
                   Row == position.Row &&
                   Column == position.Column;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        //перегрузка операторов
        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        //Сдвиг позиции
        public static Position operator +(Position pos, Direction dir) 
        { 
            return new Position(pos.Row + dir.RowDelta, pos.Column + dir.ColumnDelta);
        }
    }
}
