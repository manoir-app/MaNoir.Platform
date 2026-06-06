using MaNoir.Core.Problems;

namespace MaNoir.Core.Observability;

public sealed class LogsBackendUnavailableException : CoreProblemException
{
    public LogsBackendUnavailableException(string message) : base(message)
    {
    }

    public override int StatusCode => 503;

    public override string ProblemType => "https://manoir.app/problems/observability/logs-backend-unavailable";

    public override string ProblemTitle => "Logs backend unavailable";
}