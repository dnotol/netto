using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// An income the personal tax takes apart from the scale at a rate of its own (art. 171), a copyright
// royalty within its limits among them: never below zero, with the rate it is taxed at. It meets
// neither the tax-free amount nor the credit for low earned income's base, but it is in the total net
// income that credit is limited by (art. 289ter), and the municipal surcharge is on the total tax with
// it (art. 466). None unless given.
public sealed record IncomeTaxedApart(decimal Income, RuleAmount Rate);
