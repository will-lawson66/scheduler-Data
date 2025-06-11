namespace Instrument.Execution.Grpc;
using Instrument.Grpc;

using ProtoBuf;

/// <summary>
/// Contains the details regarding a failure that
/// occurred due to a sequence failing.
/// </summary>
/// <param name="SequenceKey">
/// A unique human readable key that identifies
/// the sequence.
/// </param>
/// <param name="ErrorDetails">
/// The error specifics to share regarding what
/// failure occurred. 
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record FailedExecutionContract(
    string SequenceKey,
    GrpcErrorContract ErrorDetails
);
