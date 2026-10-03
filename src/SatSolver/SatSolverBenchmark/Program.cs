using static System.Console;
Clear();
CursorVisible = false;

WriteLine("[V]alidate only");
WriteLine("[B]enchmark");

switch(Console.ReadKey(true).Key)
{
    case ConsoleKey.V:
        SatSolverBenchmark.SatSolverBenchmark.Validate();
        break;
    case ConsoleKey.B:
        SatSolverBenchmark.SatSolverBenchmark.Run();
        break;
}
