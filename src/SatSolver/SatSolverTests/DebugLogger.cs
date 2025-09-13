using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;

namespace SatSolverTests;

static class DebugLogger
{
    sealed class Logger : TraceListener
    {
        readonly string _path;
        readonly Thread _writingThread;
        readonly BlockingCollection<string> _buffer = [];
        public Logger(string path)
        {
            _path = path;
            _writingThread = new Thread(Writer);
            _writingThread.Start();
            Trace.Listeners.Add(this);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Trace.Listeners.Remove(this);
                _buffer.CompleteAdding();
                _writingThread.Join();
                _buffer.Dispose();
            }
            base.Dispose(disposing);
        }

        void Writer()
        {
            using var file = File.Create(_path, 2048);
            using var writer = new StreamWriter(file);
            foreach (var message in _buffer.GetConsumingEnumerable())
                writer.WriteLine(message);
            writer.Flush();
            file.Flush();
        }

        public override void Write(string? message) => throw new NotImplementedException();
        public override void WriteLine(string? message) => _buffer.Add(message ?? string.Empty);
    }

    public static IDisposable Log(string path) => new Logger(path);
}
