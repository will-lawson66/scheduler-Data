[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record FetchExecutionConfigurationRequest(bool includeSequences = true);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record FetchExecutionConfigurationRequest(
GuidRequestId,
ExecutionConfigurationContract? Configuration,
IReadOnlyCollection<GrpcErrorContract> Errors
);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record ExecutionConfigurationContract(
int StartingPeriod,
int RolloverPeriod,
TimeSpan PeriodSpan,
double PeriodAcceleration,
IReadOnlyCollection<ExecutionSequenceContract> Sequences
);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record ExecutionSequenceContract(
string Key,
TimeSpan WorstCaseTime,
string ExecutionMethod,
string ScriptKey,
IReadOnlyCollection<ExecutionResourceContract> Resources,
IReadOnlyCollection<SequenceParameterTypeContract> Parameters
);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record ExecutionResourceContract(
string Key,
bool HasScriptingInterface,
string ScriptingInterface
);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public record SequenceParameterTypeContract(
string ParameterName,
ParameterType ParameterType
) 
