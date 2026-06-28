using DavidGroup.Core.DataAccess.Results;

namespace DavidGroup.Core.DataAccess.Tests.Results;

public abstract class OperationResultTestsBase
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    protected static OperationResultMessage Info(string text = "info") =>
        new(text, OperationResultSeverity.Information);

    protected static OperationResultMessage Warn(string text = "warn") =>
        new(text, OperationResultSeverity.Warning);

    protected static OperationResultMessage Error(string text = "error") =>
        new(text, OperationResultSeverity.Error);
}
