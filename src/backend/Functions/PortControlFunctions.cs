using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;
using UniFiCameraControl.Models;
using UniFiCameraControl.Services;

namespace UniFiCameraControl.Functions;

public class PortControlFunctions
{
    private readonly ILogger<PortControlFunctions> _logger;
    private readonly IUniFiService _unifiService;

    public PortControlFunctions(
        ILogger<PortControlFunctions> logger,
        IUniFiService unifiService)
    {
        _logger = logger;
        _unifiService = unifiService;
    }

    [Function("GetPortStatus")]
    [OpenApiOperation(operationId: "GetPortStatus", tags: new[] { "Ports" }, Summary = "Get status of a specific port", Description = "Retrieves the current status of a camera port including power and PoE status.")]
    [OpenApiParameter(name: "portNumber", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The port number (1-3)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PortStatus), Description = "Port status retrieved successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Description = "Port not found")]
    public async Task<HttpResponseData> GetPortStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ports/{portNumber}/status")] HttpRequestData req,
        int portNumber)
    {
        _logger.LogInformation("Getting status for port {PortNumber}", portNumber);

        var status = await _unifiService.GetPortStatusAsync(portNumber);
        var response = req.CreateResponse();

        if (status == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteAsJsonAsync(new { error = $"Port {portNumber} not found" });
            return response;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Headers.Add("Content-Type", "application/json");
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        await response.WriteAsJsonAsync(status);
        return response;
    }

    [Function("GetAllPortsStatus")]
    [OpenApiOperation(operationId: "GetAllPortsStatus", tags: new[] { "Ports" }, Summary = "Get status of all ports", Description = "Retrieves the current status of all configured camera ports.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<PortStatus>), Description = "All port statuses retrieved successfully")]
    public async Task<HttpResponseData> GetAllPortsStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ports/status")] HttpRequestData req)
    {
        _logger.LogInformation("Getting status for all ports");

        var statuses = await _unifiService.GetAllPortsStatusAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        await response.WriteAsJsonAsync(statuses);
        return response;
    }

    [Function("SetPortState")]
    [OpenApiOperation(operationId: "SetPortState", tags: new[] { "Ports" }, Summary = "Turn a port on or off", Description = "Controls the PoE power state of a camera port.")]
    [OpenApiParameter(name: "portNumber", In = ParameterLocation.Path, Required = true, Type = typeof(int), Description = "The port number (1-3)")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(PortControlRequest), Required = true, Description = "Port control request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "Port state updated successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Description = "Invalid request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Description = "Failed to update port state")]
    public async Task<HttpResponseData> SetPortState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ports/{portNumber}/state")] HttpRequestData req,
        int portNumber)
    {
        _logger.LogInformation("Setting state for port {PortNumber}", portNumber);

        PortControlRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<PortControlRequest>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing request body");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
            return errorResponse;
        }

        if (request == null)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new { error = "Request body is required" });
            return errorResponse;
        }

        var success = await _unifiService.SetPortStateAsync(portNumber, request.Enable);
        var response = req.CreateResponse();

        if (success)
        {
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            await response.WriteAsJsonAsync(new { 
                success = true, 
                message = $"Port {portNumber} {(request.Enable ? "enabled" : "disabled")}" 
            });
        }
        else
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new { 
                success = false, 
                error = "Failed to set port state" 
            });
        }

        return response;
    }

    [Function("OptionsRequest")]
    public HttpResponseData HandleOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*route}")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        return response;
    }
}
