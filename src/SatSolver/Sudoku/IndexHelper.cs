namespace Sudoku;

static class IndexHelper
{
    public static (int column, int row) ToBoard(this int boardIndex) => (column: boardIndex / 9, row: boardIndex % 9);
    public static int ToBoardIndex(this (int column, int row) board) => board.column * 9 + board.row;

    public static (int column, int row, int number) ToSudoku(this int variableIndex) => (column: (variableIndex - 1) / 81, row: (variableIndex - 1) / 9 % 9, number: ((variableIndex - 1) % 9) + 1);
    public static int ToVariableIndex(this (int column, int row, int number) sudoku) => (sudoku.column * 9 + sudoku.row) * 9 + sudoku.number;

}
