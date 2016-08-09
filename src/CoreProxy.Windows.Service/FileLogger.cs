using System;
using System.IO;

namespace CoreProxy.Windows.Service
{
    internal sealed class FileLogger : IDisposable
    {
        private readonly StreamWriter writer;

        public FileLogger(string path)
        {
            writer = File.AppendText(path);
        }

        public void Log(string message)
        {
            writer.WriteLine($"{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss}\t{message}");
            writer.Flush();
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}