namespace Eventix.Modules.VenueModule.DTOs
{
    public class CreateVenueRequest
    {
        public string Name { get; set; } = null!;

        public string? Address { get; set; }

        public string? City { get; set; }

        public int Capacity { get; set; }
    }
}
