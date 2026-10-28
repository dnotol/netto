namespace Netto.Core.PersonalTax;

// The people the director has as dependants on 1 January of the tax year, as the user declares them on
// the tax return's cadre II B (CIR 92 art. 132, 133 and 136): Children, the dependent children (rubrique
// 1 a, code 1030); ChildrenUnder3, how many of them are under 3 with no childcare costs claimed for them,
// counted among the children too (1 c); SingleParent, whether the director, taxed alone, raises them with
// nobody but their children and relatives in the household (art. 133 al. 1, 1°, rubrique A 5);
// DependantsAged66InCare, parents, grandparents, brothers and sisters aged 66 or more in a situation of
// dependence (art. 132 al. 1, 7°, rubrique 4); and OtherDependants, every other dependant (8°, rubrique 5).
// Eligibility is the user's to declare, never the engine's to decide; refused below zero, and the children
// under 3 beyond the children (GrossToNet). None unless given.
//
// Not modelled yet, each until its article is filed and read: a disabled child or dependant counting twice
// (art. 132 al. 2, which would be a count among Children and the others here, doubled in the supplements
// and in the credit's cap of art. 134 §3 al. 3); the children whose care is shared equally, whose
// supplements are halved (art. 132bis, and its half cap); the disabled taxpayer's raise of the tax-free
// amount (art. 131 al. 3, a status of the director or the partner rather than a dependant); the single
// parent's additional supplement on a low income (art. 133 al. 2 and 3); and the supplement in the year
// of the marriage for a spouse with few resources (art. 133 al. 1, 2°).
public sealed record Dependants(
    int Children = 0,
    int ChildrenUnder3 = 0,
    bool SingleParent = false,
    int DependantsAged66InCare = 0,
    int OtherDependants = 0)
{
    public static Dependants None { get; } = new();
}
