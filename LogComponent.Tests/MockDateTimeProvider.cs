namespace LogComponent.Tests
{
    public class MockTimeProvider : IDateTimeProvider
    {
        private DateTime _currentTime;

        public MockTimeProvider(DateTime initialTime)
        {
            _currentTime = initialTime;
        }

        public DateTime Now
        {
            get => _currentTime;
            set => _currentTime = value;
        }
    }
}
