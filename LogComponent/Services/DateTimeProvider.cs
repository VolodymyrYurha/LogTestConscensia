using LogComponent.Services.Interfaces;

namespace LogComponent.Services
{
    public class SystemTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
