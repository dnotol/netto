using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// A municipality by its INS code (Statbel's REFNIS code) and its name as the source spells it,
// with its surcharge rate for one income year.
public sealed record Municipality(int InsCode, string Name, RuleAmount Rate);
