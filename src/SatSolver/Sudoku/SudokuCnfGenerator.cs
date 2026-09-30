using System.Text;

namespace Sudoku;

static class SudokuCnfGenerator
{
    public static void CreateSudokuCnf(string path)
    {
        var builder = new StringBuilder();
        var clauseCount = 0;

        void AppendExactlyOne(IEnumerable<int> variables)
        {
            var indices = variables.ToArray();
            builder.AppendLine(string.Join(" ", indices) + " 0");
            clauseCount++;

            // Explicit pairwise exclusions allow unit propagation in every group.
            for (var first = 0; first < indices.Length - 1; first++)
                for (var second = first + 1; second < indices.Length; second++)
                {
                    builder.AppendLine($"-{indices[first]} -{indices[second]} 0");
                    clauseCount++;
                }
        }

        builder.AppendLine();
        builder.AppendLine("c");
        builder.AppendLine("c Cells");
        builder.AppendLine("c");
        for (var column = 0; column < 9; column++)
            for (var row = 0; row < 9; row++)
            {
                builder.AppendLine();
                builder.AppendLine($"c Cell ({column}, {row})");
                var start = (column, row, 1).ToVariableIndex();
                AppendExactlyOne(Enumerable.Range(start, 9));
            }

        builder.AppendLine("");
        builder.AppendLine("c");
        builder.AppendLine("c Rows");
        builder.AppendLine("c");
        for (var row = 0; row < 9; row++)
        {
            builder.AppendLine();
            builder.AppendLine($"c Row {row}");
            for (var number = 1; number <= 9; number++)
                AppendExactlyOne(Enumerable.Range(0, 9).Select(column => (column, row, number).ToVariableIndex()));
        }

        builder.AppendLine("");
        builder.AppendLine("c");
        builder.AppendLine("c Columns");
        builder.AppendLine("c");
        for (var column = 0; column < 9; column++)
        {
            builder.AppendLine();
            builder.AppendLine($"c Column {column}");
            for (var number = 1; number <= 9; number++)
                AppendExactlyOne(Enumerable.Range(0, 9).Select(row => (column, row, number).ToVariableIndex()));
        }

        builder.AppendLine("");
        builder.AppendLine("c");
        builder.AppendLine("c Boxes");
        builder.AppendLine("c");
        var boxOffsets = new[] { 0, 1, 2, 9, 10, 11, 18, 19, 20 };
        var boxStarts = new[] { 0, 3, 6, 27, 30, 33, 54, 57, 60 };
        for (var box = 0; box<boxStarts.Length; box++)
        {
            builder.AppendLine();
            builder.AppendLine($"c Box {box}");
            for (var number = 1; number <= 9; number++)
                AppendExactlyOne(Enumerable.Range(0, 9).Select(i => (boxStarts[box] + boxOffsets[i]) * 9 + number));
        }

        builder.Insert(0, $"p cnf 729 {clauseCount}{Environment.NewLine}");
        File.WriteAllText(path, builder.ToString());
    }
}
