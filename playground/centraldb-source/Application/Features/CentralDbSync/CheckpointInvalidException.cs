namespace Application.Features.CentralDbSync;

public sealed class CheckpointInvalidException(string ruleName, long? currentCheckpoint, long minValidVersion)
    : InvalidOperationException($"Checkpoint invalid for {ruleName}: current={currentCheckpoint}, minValid={minValidVersion}")
{
    public string SourceTable { get; } = ruleName;
    public long? CurrentCheckpoint { get; } = currentCheckpoint;
    public long MinValidVersion { get; } = minValidVersion;
}
