using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys of the list of what the module chosen does to another's tax base or under another's
// condition (MapEffectsView), held to MapTab.resx by MapEffectTextTests, so an item without its words fails a
// test instead of showing its key on screen.
internal static class MapEffectText
{
    // The list's name, for a screen reader, which hears it before its items.
    public const string Effects = "Effects";

    // A tax base the module chosen changes, then the other module, whose it is; or one changed by another, then
    // the module it comes from: its words with what changes it and its amount.
    public static string TaxBase(bool sets) => sets ? "Effect_TaxBaseOf" : "Effect_TaxBaseFrom";

    // A condition the module chosen sets, then the module it is on; or one set on it, then the module that sets
    // it: its rule and whether it holds.
    public static string Condition(bool sets) => sets ? "Effect_ConditionOn" : "Effect_ConditionFrom";

    // The rule a condition's line is, by the modules it runs between, as MapConditions draws them; a pair no
    // condition runs between refused.
    public static string Rule(RiverModule from, RiverModule to) => (from, to) switch
    {
        (RiverModule.Allowances, RiverModule.Rent) => "Rule_NoOfficeAllowanceBesideRent",
        (RiverModule.Allowances, RiverModule.Equipment) => "Rule_NoAllowanceBesideEquipment",
        (RiverModule.Split, RiverModule.CorporateTax) => "Rule_PayKeepsReducedRate",
        (RiverModule.Car or RiverModule.Equipment, RiverModule.CorporateTax) => "Rule_BenefitsWithinShareOfPay",
        (RiverModule.Copyright, RiverModule.Split) => "Rule_RoyaltyLowersPay",
        (RiverModule.Dividend, RiverModule.Split) => "Rule_PayOutCoversReserve",
        _ => throw new ArgumentOutOfRangeException(nameof(from), $"{from} to {to}", "No condition the map draws runs between these modules."),
    };

    // A tax base's amount as the list says it: what it adds, signed.
    public static string Amount(decimal amount) => amount > 0m ? $"+{Formats.Euros(amount)}" : Formats.Euros(amount);
}
