namespace Grb.Building.Processor.Job
{
    public class ProcessWindowOptions
    {
        public const string ProcessWindowConfigurationKey = "ProcessWindow";

        public int FromHour { get; set; }
        public int UntilHour { get; set; }
    }
}
