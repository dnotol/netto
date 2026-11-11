namespace Netto.Tests.Support;

// The browser's next paint, or the edits' pause, stood in for: how many times the page waited for it, and,
// held, done only once the test releases it.
internal sealed class Wait(bool held = false)
{
    private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Asked { get; private set; }

    public ValueTask After()
    {
        Asked++;
        return held ? new ValueTask(_done.Task) : ValueTask.CompletedTask;
    }

    public void Release() => _done.SetResult();
}
