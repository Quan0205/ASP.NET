using CoffeeHRM.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ApiError(string message, int statusCode)
    {
        return StatusCode(statusCode, new ApiErrorResponse(message, statusCode));
    }
}
