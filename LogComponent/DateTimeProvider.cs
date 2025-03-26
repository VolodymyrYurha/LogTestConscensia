namespace LogComponent
{
    public class SystemTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
