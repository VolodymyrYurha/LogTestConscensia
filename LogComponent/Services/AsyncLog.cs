using LogComponent.Models;
using LogComponent.Services.Interfaces;
using System.Collections.Concurrent;

namespace LogComponent.Services
{
    public class AsyncLog : ILog
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDirectoryProvider _directoryProvider;

        private StreamWriter _writer;

        private Thread _runThread;
        private ConcurrentQueue<LogLine> _logQueue = new ConcurrentQueue<LogLine>();

        private bool _exitFlag = false;
        private bool _quitWithFlushFlag = false;
        private bool _loopFinishedFlag = false;
        private DateTime _currentDate;

        public AsyncLog(IDateTimeProvider? timeProvider = null, IDirectoryProvider? directoryProvider = null)
        {
            _dateTimeProvider = timeProvider ?? new SystemTimeProvider();
            _directoryProvider = directoryProvider ?? new DirectoryProvider();

            InitWriter();

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

                    string logContent = $"{logLine.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss:fff")}\t{logLine.LineText()}\t{Environment.NewLine}";

                    _writer.Write(logContent);
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

            SpinWait.SpinUntil(() => _loopFinishedFlag, timeout: TimeSpan.FromSeconds(5));
        }

        public void StopWithFlush()
        {
            _quitWithFlushFlag = true;

            SpinWait.SpinUntil(() => _loopFinishedFlag, timeout: TimeSpan.FromSeconds(5));
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
                InitWriter();
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

        private void InitWriter()
        {
            _currentDate = _dateTimeProvider.Now;
            _writer = File.AppendText(Path.Combine(_directoryProvider.GetLogDirectory(), "Log" + _dateTimeProvider.Now.ToString("yyyyMMdd HHmmss fff") + ".log"));
            _writer.Write("Timestamp".PadRight(25, ' ') + "\t" + "Data".PadRight(15, ' ') + "\t" + Environment.NewLine);
            _writer.AutoFlush = true;
        }

        private void FinalizeLogging()
        {
            CloseWriterIfExist();
            _loopFinishedFlag = true;
        }
    }
}