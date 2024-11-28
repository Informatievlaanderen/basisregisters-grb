namespace Grb.Building.Processor.Job
{
    public class OutsideOfficeHoursOptions
    {
        public const string OutsideOfficeHoursConfigurationKey = "OutsideOfficeHours";

        public int FromHour { get; set; }
        public int UntilHour { get; set; }
    }
}
