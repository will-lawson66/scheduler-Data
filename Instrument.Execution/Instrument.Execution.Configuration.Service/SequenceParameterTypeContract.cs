namespace Instrument.Execution.Grpc;

using Instrument.Execution.Parameter;
using ProtoBuf;

/// <summary>
/// Contract for the parameters that are part of a
/// sequence.
/// </summary>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public record SequenceParameterTypeContract(
    string ParameterName,
    ParameterType ParameterType
    ) : ISequenceParameterType;