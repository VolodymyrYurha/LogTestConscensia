using System.Collections.Concurrent;
using System.Text;

namespace LogComponent
{
    public class AsyncLog : ILog
    {
        private StreamWriter _writer;
        private IDateTimeProvider _dateTimeProvider;

        private Thread _runThread;
        private ConcurrentQueue<LogLine> _logQueue = new ConcurrentQueue<LogLine>();

        private bool _exitFlag = false;
        private bool _quitWithFlushFlag = false;
        private bool _loopFinishedFlag = false;
        private DateTime _currentDate;

        public AsyncLog(IDateTimeProvider? timeProvider = null)
        {
            _dateTimeProvider = timeProvider ?? new SystemTimeProvider();
            _currentDate = _dateTimeProvider.Now;

            if (!Directory.Exists(@"C:\LogTest"))
                Directory.CreateDirectory(@"C:\LogTest");


            _writer = File.AppendText(@"C:\LogTest\Log" + _dateTimeProvider.Now.ToString("yyyyMMdd HHmmss fff") + ".log");
            _writer.Write("TimeStamp".PadRight(25, ' ') + "\t" + "Data".PadRight(15, ' ') + "\t" + Environment.NewLine);
            _writer.AutoFlush = true;

            _runThread = new Thread(MainLoop);
            _runThread.Start();
        }

        private void MainLoop()
        {
            while (!_exitFlag)
            {
                if (_logQueue.TryDequeue(out LogLine? logLine))
                {
                    EnsureLogFileForDay(logLine.TimeStamp);
                    
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(logLine.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss:fff"));
                    stringBuilder.Append("\t");
                    stringBuilder.Append(logLine.LineText());
                    stringBuilder.Append("\t");
                    stringBuilder.Append(Environment.NewLine);

                    _writer.Write(stringBuilder.ToString());
                }

                if (_quitWithFlushFlag && _logQueue.IsEmpty)
                {
                    _exitFlag = true;
                }

                Thread.Sleep(50);
            }
            FinalizeLogging();
        }

        public void StopWithoutFlush()
        {
            _exitFlag = true;

            // Making a loop to wait for the log queue to be empty
            while (!_loopFinishedFlag)
            {
                Thread.Sleep(50);
            }
        }

        public void StopWithFlush()
        {
            _quitWithFlushFlag = true;

            // Making a loop to wait for the log queue to be empty
            while(!_loopFinishedFlag)
            {
                Thread.Sleep(50);
            }
        }

        public void Write(string text)
        {
            _logQueue.Enqueue(new LogLine() { Text = text, TimeStamp = _dateTimeProvider.Now });
        }

        private void EnsureLogFileForDay(DateTime timeStamp)
        {
            // Correct statement
            if (_currentDate.Date != timeStamp.Date)
            {
                CloseWriterIfExist();
                _currentDate = _dateTimeProvider.Now;
                _writer = File.AppendText(@"C:\LogTest\Log" + _dateTimeProvider.Now.ToString("yyyyMMdd HHmmss fff") + ".log");
                _writer.Write("Timestamp".PadRight(25, ' ') + "\t" + "Data".PadRight(15, ' ') + "\t" + Environment.NewLine);
                _writer.AutoFlush = true;
            }
        }

        private void CloseWriterIfExist()
        {
            if (_writer != null)
            {
                _writer.Flush();  
                _writer.Close(); 
                _writer.Dispose(); 
            }
        }
        private void FinalizeLogging()
        {
            CloseWriterIfExist();
            _loopFinishedFlag = true;
        }
    }
}