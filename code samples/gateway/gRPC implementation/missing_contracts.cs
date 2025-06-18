using System;
using System.Collections.Generic;
using Instrument.Execution.Grpc;
using Instrument.Grpc;
using ProtoBuf;

namespace Instrument.Execution.Grpc.Configuration;

/// <summary>
/// Request for fetching a specific sequence configuration.
/// </summary>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record FetchSequenceConfigurationRequest(
    string SequenceKey
);

/// <summary>
/// Response for fetching a specific sequence configuration.
/// </summary>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record FetchSequenceConfigurationResponse(
    Guid RequestId,
    ExecutionSequenceContract? Sequence,
    IReadOnlyCollection<GrpcErrorContract> Errors
);

/// <summary>
/// Request for fetching a specific resource configuration.
/// </summary>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record FetchResourceConfigurationRequest(
    string ResourceKey
);

/// <summary>
/// Response for fetching a specific resource configuration.
/// </summary>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record FetchResourceConfigurationResponse(
    Guid RequestId,
    ExecutionResourceContract? Resource,
    IReadOnlyCollection<GrpcErrorContract> Errors
);

/// <summary>
/// Response for updating/reloading the execution configuration.
/// </summary>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record UpdateExecutionConfigurationResponse(
    Guid RequestId,
    IReadOnlyCollection<GrpcErrorContract> Errors
);

// Fix for ExecutionConfigurationContract - add missing using statement
namespace Instrument.Execution.Grpc.Configuration
{
    using ProtoBuf;
    
    /// <summary>
    /// Updated ExecutionConfigurationContract with proper ProtoBuf using statement.
    /// </summary>
    [ProtoContract(
        ImplicitFields = ImplicitFields.AllPublic,
        SkipConstructor = true)]
    public sealed record ExecutionConfigurationContract(
        int StartingPeriod,
        int RolloverPeriod,
        TimeSpan PeriodSpan,
        double PeriodAcceleration,
        IReadOnlyCollection<ExecutionSequenceContract> Sequences);
}

// Interface for sequence parameter type to match existing code
namespace Instrument.Execution.Parameter;

public interface ISequenceParameterType
{
    string ParameterName { get; }
    ParameterType ParameterType { get; }
}

public enum ParameterType
{
    BooleanType,
    StringType,
    IntegerType,
    DecimalType,
    EnumType,
    ArrayType
}