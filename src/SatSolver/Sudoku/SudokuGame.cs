using Revo.SatSolver;
using Revo.SatSolver.Parsing;
using System.Reflection;
using System.Text;
using static System.Console;

namespace Sudoku;

sealed class SudokuGame
{
    const string boardImage = @"
╔═╤═╤═╦═╤═╤═╦═╤═╤═╗
║ │ │ ║ │ │ ║ │ │ ║
╟─┼─┼─╫─┼─┼─╫─┼─┼─╢ 
║ │ │ ║ │ │ ║ │ │ ║
╟─┼─┼─╫─┼─┼─╫─┼─┼─╢ 
║ │ │ ║ │ │ ║ │ │ ║
╠═╪═╪═╬═╪═╪═╬═╪═╪═╣
║ │ │ ║ │ │ ║ │ │ ║
╟─┼─┼─╫─┼─┼─╫─┼─┼─╢ 
║ │ │ ║ │ │ ║ │ │ ║
╟─┼─┼─╫─┼─┼─╫─┼─┼─╢ 
║ │ │ ║ │ │ ║ │ │ ║
╠═╪═╪═╬═╪═╪═╬═╪═╪═╣
║ │ │ ║ │ │ ║ │ │ ║
╟─┼─┼─╫─┼─┼─╫─┼─┼─╢ 
║ │ │ ║ │ │ ║ │ │ ║
╟─┼─┼─╫─┼─┼─╫─┼─┼─╢ 
║ │ │ ║ │ │ ║ │ │ ║
╚═╧═╧═╩═╧═╧═╩═╧═╧═╝";

    readonly Lock _sync = new();
    readonly ISatSolver _solver;
    readonly int[] _board = new int[81];
    readonly bool[] _colored = new bool[81];
    int currentX, currentY;

    CancellationTokenSource? _cancelSolve;

    SudokuGame()
    {
        using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Sudoku.sudoku.cnf")!);
        var cnf = reader.ReadToEnd();
        var problem = DimacsParser.Parse(cnf).Single();
        var options = SatSolverOptions.Sudoku;
        _solver = SatSolverFactory.Create(problem, options);
    }

    void RunInternal()
    {
        OutputEncoding = Encoding.UTF8;
        Clear();
        CursorVisible = true;
        RenderGame();
        SetCellPosition();
        
        for(; ; )
        {
            var key = Console.ReadKey(true).Key;
            lock (_sync)
            {
                if (_cancelSolve is not null)
                {
                    if (key == ConsoleKey.Escape)
                        _cancelSolve.Cancel();
                    continue;
                }
                var number = (int)key - 48;
                if (number >= 0 && number < 10)
                {
                    SetCell(currentX, currentY, number);
                    if (number > 0)
                        Console.Write(number);
                    else
                        Console.Write(' ');
                    SetCellPosition();
                    continue;
                }

                switch (key)
                {
                    case ConsoleKey.Escape:
                        Clear();
                        return;
                    case ConsoleKey.S:
                        Solve();
                        break;
                    case ConsoleKey.R:
                        RenderGame();
                        currentX = currentY = 0;
                        SetCellPosition();
                        break;
                    case ConsoleKey.C:
                        _board.AsSpan().Clear();
                        RenderGame();
                        currentX = currentY = 0;
                        SetCellPosition();
                        break;
                    case ConsoleKey.Spacebar:
                        SetCell(currentX, currentY, 0);
                        Console.Write(' ');
                        currentX = Math.Min(8, currentX+1);
                        SetCellPosition();
                        break;
                    case ConsoleKey.UpArrow:
                        currentY = Math.Max(0, currentY-1);
                        SetCellPosition();
                        break;
                    case ConsoleKey.DownArrow:
                        currentY = Math.Min(8, currentY+1);
                        SetCellPosition();
                        break;
                    case ConsoleKey.LeftArrow:
                        currentX = Math.Max(0, currentX-1);
                        SetCellPosition();
                        break;
                    case ConsoleKey.RightArrow:
                        currentX= Math.Min(8, currentX+1);
                        SetCellPosition();
                        break;
                }
            }
        }

    }

    void Solve()
    {
        _cancelSolve = new CancellationTokenSource();
        _colored.AsSpan().Clear();
        Clear();
        RenderGame();
        Task.Run(() => Solve(_cancelSolve.Token));
    }
    void Solve(CancellationToken cancellationToken)
    {
        try
        {
            lock(_sync)
            {
                Console.SetCursorPosition(0, 19);
                Console.Write(@"Solving...                 ");
                SetCellPosition();

                _solver.Reset(true);
                for (var column = 0; column<9; column++)
                    for (var row = 0; row<9; row++)
                    {
                        var number = _board[(column, row).ToBoardIndex()];
                        if (number == 0) continue;
                        _solver.AddClause(new Clause([(column, row, number).ToVariableIndex()]));
                    }
            }

            var solution = _solver.FindSolution();
            if (solution is null) 
                lock (_sync)
                {
                    Console.SetCursorPosition(0, 19);
                    ForegroundColor = ConsoleColor.Red;
                    Console.Write("No solution found.");
                    ResetColor();
                    SetCellPosition();
                    return;
                }


            var numbers = solution.Where(l => l.Sense).GroupBy(l => (l.Id-1)/9).ToDictionary(g => g.Key, g => g.Single());
            var board = numbers.OrderBy(k => k.Key).Select(k => ((k.Value.Id - 1) % 9) + 1).ToArray();

            lock(_sync)
            {
                for (var i = 0; i<_board.Length; i++)
                    _colored[i] = _board[i] == 0;
                Array.Copy(board, _board, 81);
                RenderGame();
                Console.SetCursorPosition(0, 19);
                Console.Write("          ");
                SetCellPosition();
            }
        }
        catch (OperationCanceledException)
        {
            lock (_sync)
            {
                Console.SetCursorPosition(0, 19);
                Console.Write("Canceled.");
                SetCellPosition();
                return;
            }
        }
        catch(Exception exception)
        {
            lock (_sync)
            {
                Console.SetCursorPosition(0, 19);
                ForegroundColor = ConsoleColor.Red;
                Console.Write(exception);
                ResetColor();
                SetCellPosition();
                return;
            }
        }
        finally 
        {
            lock (_sync)
            {
                _cancelSolve?.Dispose();
                _cancelSolve = null;
            }
        }
    }
    void SetCellPosition() => SetCellPosition(currentX, currentY);
    static void SetCellPosition(int col, int row) => SetCursorPosition(1 + 2 * col, 1 + 2 * row);    
    void RenderGame()
    {
        SetCursorPosition(0, 0);
        WriteLine(boardImage.Trim());
        for(var row = 0; row < 9; row++)
            for(var col = 0; col < 9; col++)
            {
                var number = GetCell(col, row);
                if (number == 0) continue;
                SetCursorPosition(1 + 2 * col, 1 + 2 * row);
                if (_colored[(col, row).ToBoardIndex()])
                    ForegroundColor = ConsoleColor.Blue;
                Write(number);
                ResetColor();
            }
    }

    int GetCell(int col, int row) => _board[(col, row).ToBoardIndex()];
    int SetCell(int col, int row, int number) => _board[(col, row).ToBoardIndex()] = number;

    public static void Run() => new SudokuGame().RunInternal();
}