using  SatSolverBenchmark;

using static System.Console;

CursorVisible = false;

WriteLine("[A]utomatic");
WriteLine("[M]anual");
WriteLine("[C]andidateQueue");
switch (ReadKey(true).Key)
{
    case ConsoleKey.A: SatSolverBenchmark.SatSolverBenchmark.Run();  break;
    case ConsoleKey.M: ManualBenchmark.Run(); break;
    case ConsoleKey.C: CandidateHeapBenchmark.Run(); break;
}
