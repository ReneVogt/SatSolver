using System.Diagnostics;
using Xunit.Abstractions;

namespace SatSolverTests;

static class DebugLogger
{
    sealed class Logger : TraceListener
    {
        readonly ITestOutputHelper? _output;
        public Logger(ITestOutputHelper? output)
        {
            _output = output;
            Trace.Listeners.Add(this);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Trace.Listeners.Remove(this);
            base.Dispose(disposing);
        }

        public override void Write(string? message) => throw new NotImplementedException();
        public override void WriteLine(string? message) => _output?.WriteLine(message);
    }

    public static IDisposable Log(ITestOutputHelper? output) => new Logger(output);
}
