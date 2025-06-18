namespace Instrument.Execution.Grpc.Configuration;

using System;
using System.Collections.Generic;
using Instrument.Grpc;
using ProtoBuf;

/// <summary>
/// Returns a resource response if one can be found.
/// </summary>
/// <param name="RequestId">
/// The request id that identifies the request that was made. 
/// </param>
/// <param name="Resource">
/// The resource if it was found (nullable).
/// </param>
/// <param name="Errors">
/// Any errors that were encountered fetching the request. 
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record FetchResourceConfigurationResponse(
    Guid RequestId,
    ExecutionResourceContract? Resource,
    IReadOnlyCollection<GrpcErrorContract> Errors);

/// <summary>
/// Contains extensions for the response that we do not want within the gRPC schema. 
/// </summary>
public static class FetchResourceConfigurationReponseExtensions
{
    /// <summary>
    /// Determines if the resource was found. 
    /// </summary>
    /// <param name="response">
    /// The response to interpret.
    /// </param>
    /// <returns>
    /// Whether or not the resource was found. 
    /// </returns>
    public static bool WasFound(this FetchResourceConfigurationResponse response)
    {
        return response.Resource is not null;
    }
}
