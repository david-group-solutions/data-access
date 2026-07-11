using DavidGroup.Core.DataAccess.Cache;
using DavidGroup.Core.DataAccess.Samples.WebApi.Additional.Dtos;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Additional.Controllers;

[ApiController]
[Route("api/cache")]
public class DistributedCacheExtensionsSamplesController(IDistributedCache cache) : ControllerBase
{
    [HttpPost("set")]
    public async Task<IActionResult> SetSample()
    {
        ProductDto product = new()
        {
            Id = Guid.NewGuid(),
            Name = "Wireless Mouse",
            Price = 29.99m,
            CreatedAt = DateTime.UtcNow
        };

        await cache.SetAsync(
            "products:sample",
            product,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }
        );

        return Ok(product);
    }

    [HttpGet("get")]
    public async Task<ActionResult<ProductDto>> GetSample()
    {
        ProductDto? product = await cache.GetAsync<ProductDto>("products:sample");

        return product is null
            ? NotFound("Item not found in cache.")
            : Ok(product);
    }

    [HttpGet("get-or-set")]
    public async Task<ActionResult<ProductDto>> GetOrSetSample()
    {
        ProductDto? product = await cache.GetOrSetAsync(
            "products:get-or-set",
            async () =>
            {
                // Simulate an expensive operation.
                await Task.Delay(1000);

                return new ProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Mechanical Keyboard",
                    Price = 89.99m,
                    CreatedAt = DateTime.UtcNow
                };
            },
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) }
        );

        return Ok(product);
    }
}
