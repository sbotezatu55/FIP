using Microsoft.AspNetCore.Http;

namespace Fip.Api.Models;

public sealed class ImportFlightRequest
{
    public IFormFile? File { get; set; }
}
