namespace LogComponent.Tests
{
    [TestFixture]
    public class AsyncLogTests
    {
        private string _logDirectory;
        private string[] _initialFiles;
        private IDirectoryProvider _directoryProvider;

        [SetUp]
        public void SetUp()
        {
            _directoryProvider = new DirectoryProvider(logType: LogType.Test);

            _logDirectory = _directoryProvider.GetLogDirectory();

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _initialFiles = GetDirectoryLogFiles();
        }

        [Test]
        public void Write_WhenCalled_ShouldWriteLogToFile()
        {
            // Arrange
            var logger = new AsyncLog(directoryProvider: _directoryProvider);
            var faker = new Faker();

            // Act
            string generatedSample = faker.Lorem.Sentence();

            logger.Write(generatedSample);

            // this method returns only after all logs are written
            // so no need in sleep
            logger.StopWithFlush();

            var latestFile = GetDirectoryLogFiles().Last();

            // Assert
            Assert.That(latestFile, Is.Not.Null, "Log file should exist.");
            var fileContent = File.ReadAllText(latestFile);
            Assert.That(fileContent.Contains(generatedSample), Is.True, "Log message should be in the file.");
        }

        [Test]
        public void Write_WhenDateChanges_ShouldCreateNewLogFile()
        {
            // Arrange
            var mockTimeProvider = new MockTimeProvider(GetRandomDateWithTime(23, 59, 50)); // 10 sec. before midnight
            var logger = new AsyncLog(mockTimeProvider, directoryProvider: _directoryProvider);
            string[] testMessages = new string[]
            {
                "First log message [23:59:00]",
                "Second log message [23:59:59]",
                "Third log message [00:00:00]",
                "Forth log message [00:00:01]"
            };

            // Act
            logger.Write(testMessages[0]);

            mockTimeProvider.Now = mockTimeProvider.Now.AddSeconds(9); // 23:59:59
            logger.Write(testMessages[1]);

            mockTimeProvider.Now = mockTimeProvider.Now.AddSeconds(1); // 00:00:00 next day
            logger.Write(testMessages[2]);

            mockTimeProvider.Now = mockTimeProvider.Now.AddSeconds(1); // 00:00:01
            logger.Write(testMessages[3]);

            logger.StopWithFlush();

            var testFiles = GetTestFiles();
            var firstFile = testFiles[0];
            var secondFile = testFiles[1];
            var firstFileContent = File.ReadAllText(firstFile);
            var secondFileContent = File.ReadAllText(secondFile);

            // Assert
            Assert.That(testFiles.Length, Is.EqualTo(2), "Exactly two testFiles should be created for two days.");
            Assert.That(firstFile, Is.Not.EqualTo(secondFile), "The log file should change when crossing midnight.");

            Assert.That(firstFileContent,
                Does.Contain(testMessages[0])
                .And.Contain(testMessages[1])
                .And.Not.Contain(testMessages[2])
                .And.Not.Contain(testMessages[3]),
                "First log file should contain only the first 2 logs.");

            Assert.That(secondFileContent,
                Does.Not.Contain(testMessages[0])
                .And.Not.Contain(testMessages[1])
                .And.Contain(testMessages[3])
                .And.Contain(testMessages[2]),
                "Second log file should contain only the last 2 logs.");
        }

        [Test]
        public void WriteWithoutFlush_WhenInterrupted_ShouldDiscardPendingLogs()
        {
            // Arrange
            var logger = new AsyncLog(directoryProvider: _directoryProvider);

            // Act
            for (int i = 50; i > 0; i--)
            {
                logger.Write("Number with No flush: " + i.ToString());
            }

            // Wait for some time to allow the thread to write couple of logs but not all of them
            Thread.Sleep(150);
            logger.StopWithoutFlush();

            var fileContent = File.ReadAllText(GetTestFiles().Last());
            var numberOfRows = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Assert
            Assert.That(fileContent, Does.Not.Contain("Number with No flush: 1"), "The last log should not be written.");
            Assert.That(fileContent, Does.Contain("Number with No flush: 50"), "This expected log should be present.");
            Assert.That(numberOfRows, Is.GreaterThan(2).And.LessThan(49), "The number of log rows should be between 2 and 49.");
        }


        [TearDown]
        public void TearDown()
        {
            var testFiles = GetTestFiles();

            foreach (var file in testFiles)
            {
                File.Delete(file);
            }
        }

        // Private helper methods
        private DateTime GetRandomDateWithTime(int hour, int minute, int second)
        {
            var random = new Random();
            DateTime startDate = new DateTime(2020, 1, 1);
            DateTime endDate = new DateTime(2030, 12, 31);

            int range = (endDate - startDate).Days;
            DateTime randomDate = startDate.AddDays(random.Next(range));

            return new DateTime(randomDate.Year, randomDate.Month, randomDate.Day, hour, minute, second);
        }
        
        private string[] GetDirectoryLogFiles()
        {
            return Directory.GetFiles(_logDirectory, "*.log").OrderBy(f => new FileInfo(f).CreationTime).ToArray();
        }
        
        private string[] GetTestFiles()
        {
            var currentFiles = GetDirectoryLogFiles();

            var testFiles = currentFiles.Except(_initialFiles).ToArray();
            return testFiles;
        }
    }
}
