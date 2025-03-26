using System.Collections.Concurrent;
using System.Text;

namespace LogComponent
{
    public class AsyncLog : ILog
    {
        private StreamWriter _writer;

        private Thread _runThread;
        private ConcurrentQueue<LogLine> _logQueue = new ConcurrentQueue<LogLine>();

        private bool _exit;
        private bool _quitWithFlush = false;
        private DateTime _currentDate = DateTime.Now;

        public AsyncLog()
        {
            if (!Directory.Exists(@"C:\LogTest"))
                Directory.CreateDirectory(@"C:\LogTest");

            _writer = File.AppendText(@"C:\LogTest\Log" + DateTime.Now.ToString("yyyyMMdd HHmmss fff") + ".log");

            _writer.Write("Timestamp".PadRight(25, ' ') + "\t" + "Data".PadRight(15, ' ') + "\t" + Environment.NewLine);
            _writer.AutoFlush = true;

            _runThread = new Thread(MainLoop);
            _runThread.Start();
        }
            
        private void MainLoop()
        {
            while (!_exit)
            {
                if (_logQueue.TryDequeue(out LogLine logLine))
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    if ((DateTime.Now - _currentDate).Days != 0)
                    {
                        _currentDate = DateTime.Now;
                        _writer = File.AppendText(@"C:\LogTest\Log" + DateTime.Now.ToString("yyyyMMdd HHmmss fff") + ".log");
                        _writer.Write("Timestamp".PadRight(25, ' ') + "\t" + "Data".PadRight(15, ' ') + "\t" + Environment.NewLine);
                        _writer.AutoFlush = true;
                    }

                    stringBuilder.Append(logLine.Timestamp.ToString("yyyy-MM-dd HH:mm:ss:fff"));
                    stringBuilder.Append("\t");
                    stringBuilder.Append(logLine.LineText());
                    stringBuilder.Append("\t");
                    stringBuilder.Append(Environment.NewLine);

                    _writer.Write(stringBuilder.ToString());
                }

                if (_quitWithFlush && _logQueue.IsEmpty)
                {
                    _exit = true;
                }

                Thread.Sleep(50);
            }
        }

        public void StopWithoutFlush()
        {
            _exit = true;
        }

        public void StopWithFlush()
        {
            _quitWithFlush = true;
        }

        public void Write(string text)
        {
            _logQueue.Enqueue(new LogLine() { Text = text, Timestamp = DateTime.Now });
        }
    }
}