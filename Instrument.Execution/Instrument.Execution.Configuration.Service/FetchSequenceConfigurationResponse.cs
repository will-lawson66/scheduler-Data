namespace Instrument.Execution.Grpc.Configuration;

using System;
using System.Collections.Generic;
using Instrument.Grpc;
using ProtoBuf;

/// <summary>
/// Returns a sequence configuration if one can be found.
/// </summary>
/// <param name="RequestId">
/// The request id that identifies the request that was made. 
/// </param>
/// <param name="Sequence">
/// The sequence if it was found (nullable).
/// </param>
/// <param name="Errors">
/// Any errors that were encountered fetching the request. 
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record FetchSequenceConfigurationResponse(
    Guid RequestId,
    ExecutionSequenceContract? Sequence,
    IReadOnlyCollection<GrpcErrorContract> Errors);

/// <summary>
/// Contains extensions for the response that we do not want within the gRPC schema. 
/// </summary>
public static class FetchSequenceConfigurationResponseExtensions
{
    /// <summary>
    /// Determines if the sequence was found. 
    /// </summary>
    /// <param name="response">
    /// The response to interpret.
    /// </param>
    /// <returns>
    /// Whether or not the sequence was found. 
    /// </returns>
    public static bool WasFound(this FetchSequenceConfigurationResponse response)
    {
        return response.Sequence is not null;
    }
}
