namespace Instrument.Execution.Grpc;
using System;
using System.Collections.Generic;
using ProtoBuf;

/// <summary>
/// A gRPC contract for execution sequences.
/// </summary>
/// <param name="Key">
/// A human readable key that uniquely identifies the
/// sequence.
/// </param>
/// <param name="WorstCaseTime">
/// A worst case time that the sequence has to run until
/// it is considered to be in error.
/// </param>
/// <param name="ScriptKey">
/// The script to use in conjunction with the <see cref="IsScripted"/>
/// parameter.
/// </param>
/// <param name="ExecutionMethod">
/// Indicates if this sequence is configured to run with a script, simulated
/// or through the native engine.
/// </param>
/// <param name="Resources">
/// The resources that the sequence utilizes when being executed.
/// </param>
/// /// <param name="Parameters">
/// The parameters that the sequence requires to be set when executing the sequence.
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record ExecutionSequenceContract(
    string Key,
    TimeSpan WorstCaseTime,
    string ExecutionMethod,
    string ScriptKey,
    IReadOnlyCollection<ExecutionResourceContract> Resources,
    IReadOnlyCollection<SequenceParameterTypeContract> Parameters
);
