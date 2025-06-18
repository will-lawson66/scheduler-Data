namespace Instrument.Execution.Grpc.Configuration;

using System;
using System.Collections.Generic;
using Instrument.Grpc;
using ProtoBuf;

/// <summary>
/// The gRPC response for fetching configuration data.
/// </summary>
/// <param name="RequestId">
/// An identifier that uniquely identifies the request. 
/// </param>
/// <param name="Configuration">
/// The current configuration when the request was made.
/// </param>
/// <param name="Errors">
/// Any errors that were encountered.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record FetchExecutionConfigurationResponse(
    Guid RequestId,
    ExecutionConfigurationContract? Configuration,
    IReadOnlyCollection<GrpcErrorContract> Errors
);