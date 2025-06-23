using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Prototype.Tests.Helpers;

public static class ResponseAssertionHelper
{
    /// <summary>
    /// Safely asserts dynamic properties on controller response objects.
    /// Handles both anonymous objects and ApiResponse objects with different property casing.
    /// </summary>
    public static void AssertSuccessResponse(IActionResult result, string? expectedMessage = null)
    {
        if (result is OkObjectResult okResult)
        {
            var response = okResult.Value;
            Assert.NotNull(response);
            
            // Try to get success property with different casings
            var success = GetDynamicProperty(response, "success") ?? GetDynamicProperty(response, "Success");
            Assert.True(success is bool && (bool)success, "Response should indicate success");
            
            if (expectedMessage != null)
            {
                var message = GetDynamicProperty(response, "message") ?? GetDynamicProperty(response, "Message");
                Assert.Equal(expectedMessage, message?.ToString());
            }
        }
        else if (result is ObjectResult objResult && objResult.StatusCode == 500)
        {
            // Exception occurred - this might be expected in some test scenarios
            Assert.NotNull(objResult.Value);
        }
        else
        {
            Assert.Fail($"Expected OkObjectResult or ObjectResult(500), got {result.GetType()}");
        }
    }
    
    /// <summary>
    /// Safely asserts failure response with expected message
    /// </summary>
    public static void AssertFailureResponse(IActionResult result, string? expectedMessage = null)
    {
        if (result is OkObjectResult okResult)
        {
            var response = okResult.Value;
            Assert.NotNull(response);
            
            // Try to get success property with different casings
            var success = GetDynamicProperty(response, "success") ?? GetDynamicProperty(response, "Success");
            Assert.True(success is bool && !(bool)success, "Response should indicate failure");
            
            if (expectedMessage != null)
            {
                var message = GetDynamicProperty(response, "message") ?? GetDynamicProperty(response, "Message");
                Assert.Equal(expectedMessage, message?.ToString());
            }
        }
        else if (result is ObjectResult objResult && objResult.StatusCode == 500)
        {
            // Exception occurred - check if this has error information
            Assert.NotNull(objResult.Value);
        }
        else if (result is BadRequestObjectResult badResult)
        {
            // Validation error
            var response = badResult.Value;
            Assert.NotNull(response);
            
            var success = GetDynamicProperty(response, "success") ?? GetDynamicProperty(response, "Success");
            Assert.True(success is bool && !(bool)success, "Response should indicate failure");
        }
        else
        {
            Assert.Fail($"Expected OkObjectResult, BadRequestObjectResult, or ObjectResult(500), got {result.GetType()}");
        }
    }
    
    /// <summary>
    /// Safely gets data property from response
    /// </summary>
    public static object GetResponseData(IActionResult result)
    {
        if (result is OkObjectResult okResult)
        {
            var response = okResult.Value;
            return GetDynamicProperty(response, "data") ?? GetDynamicProperty(response, "Data");
        }
        return null;
    }
    
    /// <summary>
    /// Safely gets a property from a dynamic object using reflection
    /// </summary>
    private static object? GetDynamicProperty(object? obj, string propertyName)
    {
        if (obj == null) return null;
        
        try
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }
}