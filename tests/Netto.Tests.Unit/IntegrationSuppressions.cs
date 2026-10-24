using System.Diagnostics.CodeAnalysis;

// The one warning this branch leaves to the story's integration: PlanStorageTests is rewritten by another
// slice, which fixes it there. Delete this file when that rewrite lands.
[assembly: SuppressMessage("Performance", "CA1826:Do not use Enumerable methods on indexable collections",
    Justification = "PlanStorageTests is rewritten by another slice of the story; its warning is fixed when it lands.",
    Scope = "member", Target = "~M:Netto.Tests.WebClient.River.PlanStorageTests.Namur")]
