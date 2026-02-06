
using TimelapseCreator.Configuration;
using Common.Classes.Configuration;

namespace TimelapseCreator
{
    public class Settings
    {
        public int DelayTimeInSeconds { get; set; }

        public int ImagesPerSecond { get; set; }

        public int SizeOfTimelapseInMb { get; set; }

        public List<TimelapseConfiguration> TimelapseConfiguration { get; set; }
    }
}
