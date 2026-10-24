namespace Netto.Core.Rules;

// What the rules refuse of what the user gives: a stable key, which the page turns into words
// (Error_<key>) and which reads as the refusal written into a string, and the input it refuses, which
// says where the page shows it. Each refusal is a static field beside the rule that refuses it, and only
// Core declares one, so every refusal the page can meet is the engine's own, with its input.
public sealed record Refusal
{
    internal Refusal(string key, RefusedInput input)
    {
        Key = key;
        Input = input;
    }

    public string Key { get; }

    public RefusedInput Input { get; }

    public override string ToString() => Key;
}
