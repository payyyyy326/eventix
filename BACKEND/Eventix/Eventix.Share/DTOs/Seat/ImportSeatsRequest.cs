using Microsoft.AspNetCore.Http;

namespace Eventix.Share.Seat
{
    public class ImportSeatsRequest
    {
        public IFormFile File { get; set; } = null!;

        public bool OverrideExisting { get; set; } = false;
    }
}
