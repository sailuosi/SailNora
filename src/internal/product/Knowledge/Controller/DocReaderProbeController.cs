using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Product.Knowledge.DocReader;

namespace SailNora.Product.Knowledge.Controller;

[ApiController]
[Route("api/v1/system/docreader")]
[Authorize]
[ApiResult]
public sealed class DocReaderProbeController : ControllerBase
{
    private readonly IDocReaderClient _client;

    public DocReaderProbeController(IDocReaderClient client) => _client = client;

    [HttpGet("info")]
    [Permission("skeleton:probe")]
    public async Task<ActionResult<DocReaderInfoResult>> Info(CancellationToken cancellationToken) =>
        Ok(await _client.GetInfoAsync(cancellationToken));
}
