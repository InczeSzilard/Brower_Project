using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Lab3Window
{
    // Writes lines with date and time into the log file
    public class AppLogger
    {
        public AppLogger(string file)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(file));
            Trace.AutoFlush = true;
        }

        // Runs on a background thread so the window doesn't wait for the disk
        public async Task LogAsync(string message)
        {
            await Task.Run(() =>
            {
                string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
                Trace.WriteLine($"{timestamp} - {message}");
            });
        }
    }
}