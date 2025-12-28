using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// INVALID: Controller that uses {id} routes instead of {idKey}.
/// This violates Rule 04: API Design and should be detected.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BadController : ControllerBase
{
    /// <summary>
    /// VIOLATION: Route uses {id} instead of {idKey}.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // This method signature violates the IdKey requirement
        return Ok(new { id, name = "Test" });
    }

    /// <summary>
    /// VIOLATION: Method parameter is int id instead of string idKey.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] object request)
    {
        return Ok(new { id });
    }

    /// <summary>
    /// VIOLATION: Another integer ID route.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        return NoContent();
    }
}

/// <summary>
/// INVALID: Controller with mixed violations.
/// </summary>
[ApiController]
[Route("api/v1/items")]
public class ItemsController : ControllerBase
{
    /// <summary>
    /// VIOLATION: Uses integer ID in URL parameter.
    /// </summary>
    [HttpGet("{itemId:int}")]
    public IActionResult GetItem(int itemId)
    {
        return Ok(new { itemId });
    }
}
