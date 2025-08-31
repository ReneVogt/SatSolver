namespace Sudoku;

static class Program
{
    static void Main()
    {       
        SudokuGame.Run();
    }

    //static void CreateSudokuCnf()
    //{
    //    var builder = new StringBuilder();
    //    builder.AppendLine("p cnf 729 3240");
    //    builder.AppendLine();
    //    builder.AppendLine("c");
    //    builder.AppendLine("c Cells");
    //    builder.AppendLine("c");
    //    for (var column = 0; column < 9; column++)
    //        for (var row = 0; row < 9; row++)
    //        {
    //            builder.AppendLine();
    //            builder.AppendLine($"c Cell ({column}, {row})");
    //            var start = (column, row, 1).ToVariableIndex();
    //            builder.AppendLine(string.Join(" ", Enumerable.Range(start, 9)) + " 0");
    //            for (var number1 = 0; number1 < 8; number1++)
    //                for (var number2 = number1 + 1; number2 < 9; number2++)
    //                    builder.AppendLine($"-{start + number1} -{start + number2} 0");                
    //        }

    //    builder.AppendLine("");
    //    builder.AppendLine("c");
    //    builder.AppendLine("c Rows");
    //    builder.AppendLine("c");
    //    for (var row = 0; row < 9; row++)
    //    {
    //        builder.AppendLine();
    //        builder.AppendLine($"c Row {row}");
    //        for (var number = 1; number <= 9; number++)
    //            builder.AppendLine(string.Join(" ", Enumerable.Range(0, 9).Select(column => (column, row, number).ToVariableIndex())) + " 0");
    //    }

    //    builder.AppendLine("");
    //    builder.AppendLine("c");
    //    builder.AppendLine("c Columns");
    //    builder.AppendLine("c");
    //    for (var column = 0; column < 9; column++)
    //    {
    //        builder.AppendLine();
    //        builder.AppendLine($"c Column {column}");
    //        for (var number = 1; number <= 9; number++)
    //            builder.AppendLine(string.Join(" ", Enumerable.Range(0, 9).Select(row => (column, row, number).ToVariableIndex())) + " 0");
    //    }

    //    builder.AppendLine("");
    //    builder.AppendLine("c");
    //    builder.AppendLine("c Boxes");
    //    builder.AppendLine("c");
    //    var boxOffsets = new[] { 0, 1, 2, 9, 10, 11, 18, 19, 20 };
    //    var boxStarts = new[] { 0, 3, 6, 27, 30, 33, 54, 57, 60 };
    //    for (var box = 0; box<boxStarts.Length; box++)
    //    {
    //        builder.AppendLine();
    //        builder.AppendLine($"c Box {box}");
    //        for (var number = 1; number <= 9; number++)
    //            builder.AppendLine(string.Join(" ", Enumerable.Range(0, 9).Select(i => (boxStarts[box] + boxOffsets[i]) * 9 + number)) + " 0");
    //    }

    //    File.WriteAllText("sudoku.cnf", builder.ToString());
    //}
}
