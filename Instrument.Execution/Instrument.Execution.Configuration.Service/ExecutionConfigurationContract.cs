namespace Instrument.Execution.Grpc.Configuration;

using System;
using System.Collections.Generic;
using ProtoBuf;

/// <summary>
/// 
/// </summary>
/// <param name="StartingPeriod">
/// The value to start initial period of the
/// execution engine at. Increments after the
/// timespan specified in <see cref="PeriodSpan"/>
/// </param>
/// <param name="RolloverPeriod">
/// The period to start counting again from the
/// starting period. 
/// </param>
/// <param name="PeriodSpan">
/// The duration of a period. 
/// </param>
/// <param name="PeriodAcceleration">
/// A multiplier to apply to the <see cref="PeriodSpan"/>
/// to support acceleration.
/// </param>
/// <param name="Sequences">
/// All allowable sequences and their configurations. 
/// </param>
[ProtoContract(
    ImplicitFields = ImplicitFields.AllPublic,
    SkipConstructor = true)]
public sealed record ExecutionConfigurationContract(
    int StartingPeriod,
    int RolloverPeriod,
    TimeSpan PeriodSpan,
    double PeriodAcceleration,
    IReadOnlyCollection<ExecutionSequenceContract> Sequences);