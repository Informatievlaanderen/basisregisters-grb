namespace Grb.Building.Processor.Upload
{
    public class EcsTaskOptions
    {
        public required string TaskDefinition { get; set; }
        public required string Cluster { get; set; }
        public required string Subnets { get; set; }
        public required string SecurityGroups { get; set; }
    }
}
