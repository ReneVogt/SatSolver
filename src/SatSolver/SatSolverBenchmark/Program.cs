using  SatSolverBenchmark;

using static System.Console;

WriteLine("[A]utomatic or [M]anual? ");
switch (ReadKey(true).Key)
{
    case ConsoleKey.A: Benchmark.Run();  break;
    case ConsoleKey.M: ManualBenchmark.Run(); break;
}
