using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace HRMgmtTest.utils;

/// <summary>
/// Retry attribute for flaky UI tests in CI environments.
/// Retries failing tests up to a specified number of times before
/// marking them as failed.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class RetryAttribute : NUnitAttribute, IRepeatTest
{
    private readonly int _tryCount;

    /// <summary>
    /// Construct a RetryAttribute.
    /// </summary>
    /// <param name="tryCount">The maximum number of times to run the test.</param>
    public RetryAttribute(int tryCount)
    {
        _tryCount = tryCount;
    }

    /// <summary>
    /// Wrap a command and return the result.
    /// </summary>
    public TestCommand Wrap(TestCommand command)
    {
        return new RetryCommand(command, _tryCount);
    }

    private class RetryCommand : DelegatingTestCommand
    {
        private readonly int _tryCount;

        public RetryCommand(TestCommand innerCommand, int tryCount)
            : base(innerCommand)
        {
            _tryCount = tryCount;
        }

        public override TestResult Execute(TestExecutionContext context)
        {
            var count = _tryCount;

            while (count-- > 0)
            {
                try
                {
                    context.CurrentResult = innerCommand.Execute(context);
                }
                catch (Exception ex)
                {
                    context.CurrentResult ??= context.CurrentTest.MakeTestResult();
                    context.CurrentResult.RecordException(ex);
                }

                if (context.CurrentResult.ResultState != ResultState.Failure &&
                    context.CurrentResult.ResultState != ResultState.Error)
                {
                    break;
                }

                // Still have retries left - reset and try again
                if (count > 0)
                {
                    TestContext.Progress.WriteLine(
                        $"[Retry] Test '{context.CurrentTest.Name}' failed. Retrying... ({count} attempt(s) remaining)");

                    // Allow some stabilization time between retries
                    Thread.Sleep(1000);
                }
            }

            return context.CurrentResult;
        }
    }
}
