namespace Eventix.Modules.SeatModule.DTOs
{
    public class ImportSeatsRequest
    {
        public IFormFile File { get; set; } = null!;

        public bool OverrideExisting { get; set; } = false;
    }
}
