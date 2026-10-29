using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the module chosen does to another module's tax base or under another's condition, and what is done to it
// so: an item for each line of a tax base or a condition the map draws for it, in the order of the map's lines,
// each saying which way it goes, a tax base its amount, the very amount of its line, and what changes it, the card
// lines its line holds, a condition its rule and its word, and the other module.
public class MapEffectsViewTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static MapView View(RiverInputs river) => MapView.From(river, river.Run());

    // Every item is a line of a tax base or a condition the map draws for the module chosen, in their order.
    [Theory]
    [InlineData(RiverModule.Car)]
    [InlineData(RiverModule.CorporateTax)]
    [InlineData(RiverModule.DirectorPay)]
    [InlineData(RiverModule.Household)]
    [InlineData(RiverModule.Split)]
    public void Of_aModuleChosen_isEachRulesLineTheMapDrawsForItInTheirOrder(RiverModule chosen)
    {
        var view = View(Full);
        var lines = view.ShowingFor(chosen).Bundles().Where(line => !MapLinkStyle.IsMoney(line.Kind)).ToList();

        var effects = MapEffectsView.Of(view, chosen);

        effects.Items.ShouldSatisfyAllConditions(
            () => effects.Items.ShouldNotBeEmpty(),
            () => effects.Items.Select(item => item.Route).ShouldBe(lines.Select(line => line.Route)),
            () => effects.Items.Select(item => item.Other).ShouldBe(lines.Select(line => line.From.Module == chosen ? line.To.Module!.Value : line.From.Module!.Value)),
            () => effects.Items.ShouldAllBe(item => !item.Marked));
    }

    // The car chosen: its benefit taxed as Director pay's pay, what Corporate tax adds back of it, and the rule
    // it is held to on Corporate tax's rate, each said from the car, its amount the line's.
    [Fact]
    public void Of_theCarChosen_saysWhatItAddsToEachTaxBaseAndTheRuleItIsHeldTo()
    {
        var view = View(Full);
        var benefit = MapViewTests.Line(view, MapEnd.Of(RiverModule.Car), MapEnd.Of(RiverModule.DirectorPay));
        var addBack = view.Bundles().Single(line => line.Kind == MapLinkKind.TaxBase && line.From == MapEnd.Of(RiverModule.Car) && line.To == MapEnd.Of(RiverModule.CorporateTax));
        var rule = view.Bundles().Single(line => line.Kind == MapLinkKind.Condition && line.From == MapEnd.Of(RiverModule.Car));

        var items = MapEffectsView.Of(view, RiverModule.Car).Items;

        items.Select(item => (item.Key, item.Rule?.Key, item.State?.Key, item.Amount, string.Join(", ", item.What.Select(what => what.Key)), item.Other)).ShouldBe([
            ("Effect_TaxBaseOf", null, null, $"+{Formats.Euros(benefit.Amount!.Value)}", string.Join(", ", benefit.Links.Select(link => link.Carries.Key)), RiverModule.DirectorPay),
            ("Effect_TaxBaseOf", null, null, $"+{Formats.Euros(addBack.Amount!.Value)}", string.Join(", ", addBack.Links.Select(link => link.Carries.Key)), RiverModule.CorporateTax),
            ("Effect_ConditionOn", "Rule_BenefitsWithinShareOfPay", rule.Links[0].State!.Key, null, "", RiverModule.CorporateTax),
        ], ignoreOrder: true);
    }

    // Corporate tax chosen: the conditions set on its rate are said as set by the split and the car, the tax
    // bases as from the vouchers and the car.
    [Fact]
    public void Of_corporateTaxChosen_saysWhatIsSetOnItAndAddedToItsTaxBaseFromWhere()
    {
        var items = MapEffectsView.Of(View(Full), RiverModule.CorporateTax).Items;

        items.Select(item => (item.Key, item.Other)).ShouldBe([
            ("Effect_ConditionFrom", RiverModule.Split), ("Effect_ConditionFrom", RiverModule.Car),
            ("Effect_TaxBaseFrom", RiverModule.Vouchers), ("Effect_TaxBaseFrom", RiverModule.Car)
        ], ignoreOrder: true);
    }

    // The partner chosen: the marital quotient, from Director pay, the Household card's line.
    [Fact]
    public void Of_thePartnerChosen_isTheMaritalQuotientFromDirectorPay()
    {
        var view = View(Full);
        var quotient = view.Bundles().Single(line => line.Kind == MapLinkKind.TaxBase && line.Card == RiverModule.Household);

        var item = MapEffectsView.Of(view, RiverModule.Household).Items.ShouldHaveSingleItem();

        (item.Key, item.Amount, item.What.Select(what => what.Key).Single(), item.Other)
            .ShouldBe(("Effect_TaxBaseFrom", $"+{Formats.Euros(quotient.Amount!.Value)}", quotient.Links[0].Carries.Key, RiverModule.DirectorPay));
    }

    // A module that neither changes a tax base nor sets or meets a condition has none; nor has any module while
    // the engine refuses an amount.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Of_aModuleWithNoRulesLine_isNone(bool refused)
    {
        var river = refused ? Full.WithReserve(-1m) : Full;

        MapEffectsView.Of(View(river), refused ? RiverModule.Car : RiverModule.CompanyYear).Items.ShouldBeEmpty();
    }

    // The item of the line under the viewer's pointer is marked, it alone; none for no line.
    [Fact]
    public void Marking_aLine_marksItsItemAlone()
    {
        var effects = MapEffectsView.Of(View(Full), RiverModule.Car);

        var (marked, none) = (effects.Marking(effects.Items[1].Route), effects.Marking(null));

        (string.Join(' ', marked.Items.Select(item => item.Marked)), none.Items.Any(item => item.Marked)).ShouldBe(("False True False", false));
    }
}
