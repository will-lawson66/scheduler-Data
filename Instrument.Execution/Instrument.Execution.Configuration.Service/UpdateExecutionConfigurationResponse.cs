namespace Instrument.Execution.Grpc.Configuration;

using System;
using System.Collections.Generic;
using Instrument.Grpc;
using ProtoBuf;

/// <summary>
/// The gRPC response for when an update to the execution
/// engine was updated. 
/// </summary>
/// <param name="Errors">
/// Any errors that were encountered.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record UpdateExecutionConfigurationResponse(
    Guid RequestId,
    IReadOnlyCollection<GrpcErrorContract> Errors
);
