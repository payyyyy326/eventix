namespace Eventix.Modules.SeatModule.DTOs
{
    public class ImportSeatResult
    {
        public int TotalRows { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
