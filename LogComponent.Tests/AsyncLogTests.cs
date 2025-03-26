namespace LogComponent.Tests
{
    [TestFixture]
    public class AsyncLogTests
    {
        private string _logDirectory;
        private string[] _initialFiles;

        [SetUp]
        public void SetUp()
        {
            _logDirectory = @"C:\LogTest";

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _initialFiles = Directory.GetFiles(_logDirectory, "*.log");
        }

        [Test]
        public void WriteLog_ShouldWriteToNewFile()
        {
            // Arrange
            var logger = new AsyncLog();
            var faker = new Faker();

            // Act
            string generatedSample = faker.Lorem.Sentence();

            logger.Write(generatedSample);

            // this method returns only after all logs are written
            // so no need in sleep
            logger.StopWithFlush();

            var files = Directory.GetFiles(_logDirectory, "*.log");
            var latestFile = GetLatestFile(files);

            // Assert
            Assert.That(latestFile, Is.Not.Null, "Log file should exist.");
            var fileContent = File.ReadAllText(latestFile);
            Assert.That(fileContent.Contains(generatedSample), Is.True, "Log message should be in the file.");
        }

        [TearDown]
        public void TearDown()
        {
            var filesAfterTest = Directory.GetFiles(_logDirectory, "*.log");

            var newFiles = filesAfterTest.Except(_initialFiles).ToList();

            foreach (var file in newFiles)
            {
                File.Delete(file);
            }
        }

        private string GetLatestFile(string[] files)
        {
            return files.OrderByDescending(f => new FileInfo(f).CreationTime).First();
        }
    }
}
