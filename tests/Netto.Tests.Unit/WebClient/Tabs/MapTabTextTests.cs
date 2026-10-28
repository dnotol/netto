using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab's texts, held to MapTab.resx, and the words of its links to the texts that word them, the
// drain lists' and the cards': a misspelt key would show on screen as the key itself.
public class MapTabTextTests
{
    // The name IStringLocalizer<MapTab> reads Tabs/MapTab.resx under.
    private static readonly ResxText Resources = ResxText.For<MapTab>();

    public static TheoryData<RiverModule> Modules => new(MapModules.Order);

    // The card each module of the map is.
    public static Type Card(RiverModule module) => module switch
    {
        RiverModule.CompanyYear => typeof(CompanyYearCard),
        RiverModule.Vouchers => typeof(VoucherCard),
        RiverModule.Allowances => typeof(AllowanceCard),
        RiverModule.Rent => typeof(RentCard),
        RiverModule.Car => typeof(CarCard),
        RiverModule.Split => typeof(SplitCard),
        RiverModule.Copyright => typeof(CopyrightCard),
        RiverModule.DirectorPay => typeof(DirectorPayCard),
        RiverModule.CorporateTax => typeof(CorporateTaxCard),
        RiverModule.Dividend => typeof(DividendCard),
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null),
    };

    [Fact]
    public void MapTabResx_head_saysWhatTheMapIs()
    {
        (Resources.Get("Eyebrow"), Resources.Get("Title")).ShouldBe(("Your year · Income year 2026", "From what your company earns to what reaches you"));
    }

    // A module on the map is named as its card is.
    [Theory]
    [MemberData(nameof(Modules))]
    public void MapTabResx_aModule_isNamedByItsCardsTitle(RiverModule module)
    {
        Resources.Get(MapTabText.Module(module)).ShouldBe(ResxText.For(Card(module)).Get("Title"));
    }

    // A condition's words by the key its helper builds, kind by kind and with each variant: a test of the
    // rate that holds says its condition is met, one that fails that the rate is lost.
    public static TheoryData<string, string[], string> ConditionWords => new()
    {
        { MapTabText.PayCondition(ReducedRateCondition.MinimumPayMet), ["€60,000.00", "€51,000.00", "€40,000.00"], "Pay €60,000.00 reaches the €51,000.00 minimum: pay condition met" },
        { MapTabText.PayCondition(ReducedRateCondition.TaxableIncomeNotAbovePay), ["€50,000.00", "€51,000.00", "€50,000.00"], "Pay €50,000.00 not below the €50,000.00 taxable profit: pay condition met" },
        { MapTabText.PayCondition(ReducedRateCondition.MinimumPayNotMet), ["€40,000.00", "€51,000.00", "€60,000.00"], "Pay €40,000.00 below the €51,000.00 minimum and the €60,000.00 taxable profit: reduced rate lost" },
        { MapTabText.BenefitsCondition(true), ["€1,714.29", "20%", "€51,714.29"], "Benefits in kind €1,714.29 within 20% of the €51,714.29 pay: benefits condition met" },
        { MapTabText.BenefitsCondition(false), ["€1,714.29", "20%", "€6,714.29"], "Benefits in kind €1,714.29 over 20% of the €6,714.29 pay: reduced rate lost" },
        { MapTabText.OfficeCondition(rentPaid: true), [], "No office allowance beside the rent: kept" },
        { MapTabText.OfficeCondition(rentPaid: false), [], "No rent beside the office allowance: kept" },
        { MapTabText.RoyaltyCondition(new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false)), ["€40,000.00", "€50,000.00"], "Lowers the pay keeping the reduced rate to €40,000.00 (€50,000.00 without)" },
        { MapTabText.RoyaltyCondition(new RoyaltyLowersPayKeepingReducedRate(40_000m, null, false)), ["€40,000.00"], "Lets a pay of €40,000.00 keep the reduced rate (none without)" },
        { MapTabText.RoyaltyCondition(new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false)), ["€51,000.00", "€51,000.00"], "Does not lower the pay keeping the reduced rate, €51,000.00" },
        { MapTabText.RoyaltyCondition(new RoyaltyLowersPayKeepingReducedRate(null, 50_000m, false)), ["€50,000.00"], "No pay keeps the reduced rate beside it" },
        { MapTabText.ReserveCondition(true), ["€40,000.00", "€40,000.00"], "Reserve €40,000.00 covered: up to €40,000.00 to pay out" },
        { MapTabText.ReserveCondition(false), ["€40,000.01", "€40,000.00"], "Reserve €40,000.01 not covered: only €40,000.00 to pay out" },
    };

    [Theory]
    [MemberData(nameof(ConditionWords))]
    public void MapTabResx_aCondition_isWordedByTheKeyItsHelperBuilds(string key, string[] args, string words)
    {
        Resources.Format(key, args).ShouldBe(words);
    }

    // Whether a condition holds in a word or two, by the key its helper builds, each kind with each
    // variant: the word its line says on the map.
    public static TheoryData<string, string> StateWords => new()
    {
        { MapTabText.RateState(holds: true), "met" },
        { MapTabText.RateState(holds: false), "lost" },
        { MapTabText.OfficeState, "kept" },
        { MapTabText.RoyaltyState(new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false)), "lowers" },
        { MapTabText.RoyaltyState(new RoyaltyLowersPayKeepingReducedRate(40_000m, null, false)), "lowers" },
        { MapTabText.RoyaltyState(new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false)), "no effect" },
        { MapTabText.RoyaltyState(new RoyaltyLowersPayKeepingReducedRate(null, 50_000m, false)), "no pay keeps" },
        { MapTabText.ReserveState(holds: true), "covered" },
        { MapTabText.ReserveState(holds: false), "not covered" },
    };

    [Theory]
    [MemberData(nameof(StateWords))]
    public void MapTabResx_aConditionsState_isAWordOrTwo(string key, string words)
    {
        Resources.Get(key).ShouldBe(words);
    }

    // A badge in a module's box, by the key its helper builds, each kind with each variant: which rule and
    // whether it holds, with the state its line says; what a tax base gains, its amount first. Each fits a
    // box on one line, 30 characters at most, a tax base's at an amount of millions.
    public static TheoryData<string, string[], string> BadgeWords => new()
    {
        { MapTabText.PayBadge(holds: true), [], "pay met" },
        { MapTabText.PayBadge(holds: false), [], "rate lost: pay" },
        { MapTabText.BenefitsBadge(holds: true), [], "benefits met" },
        { MapTabText.BenefitsBadge(holds: false), [], "rate lost: benefits" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false)), [], "royalty lowers pay" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(40_000m, null, false)), [], "royalty lowers pay" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false)), [], "royalty: no effect" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(null, 50_000m, false)), [], "no pay keeps rate" },
        { MapTabText.ReserveBadge(holds: true), [], "reserve covered" },
        { MapTabText.ReserveBadge(holds: false), [], "reserve not covered" },
        { MapTabText.TaxBaseBadge, ["€9,999,999.99"], "+€9,999,999.99 to tax base" },
    };

    [Theory]
    [MemberData(nameof(BadgeWords))]
    public void MapTabResx_aBadge_isWordedByTheKeyItsHelperBuildsInThirtyCharactersAtMost(string key, string[] args, string words)
    {
        var text = Resources.Format(key, args);

        (text, text.Length <= 30).ShouldBe((words, true));
    }

    // The column beside the map has a component and a .resx of its own: none of the texts of the list it
    // replaced is in this one any more.
    [Fact]
    public void MapTabResx_theTextsOfTheListTheNeighbourhoodReplaced_areGone()
    {
        Resources.Keys.Intersect(["Link", "LinkWithoutAmount", "LinksOf", "LinksOfNone", "ChooseHint", "NoLinks", "NoFigures", "Hidden_One", "Hidden_Many"]).ShouldBeEmpty();
    }

    // Every link of these plans is worded by the texts its label names, its arguments taken.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Carries_everyLinkOfAPlan_isWordedByItsTexts(bool companyPays)
    {
        var river = companyPays ? MapLinksTests.Full.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.035m) : MapLinksTests.Full;

        MapLinks.Of(river, river.Run()).Select(link => link.Carries)
            .Where(label => ResxText.For(MapTabText.Texts(label.Text)).Get(label.Key) is not { } text
                || (label.Args.Count > 0 && !text.Contains("{0}", StringComparison.Ordinal)))
            .Select(label => $"{label.Text} {label.Key}").ShouldBeEmpty();
    }

    // Each kind of text a link is worded by is a component's own, its .resx holding texts.
    [Fact]
    public void Texts_everyKind_isAComponentsTexts()
    {
        Enum.GetValues<MapText>().Where(text => ResxText.For(MapTabText.Texts(text)).Keys.Count == 0).ShouldBeEmpty();
    }

    [Fact]
    public void Texts_theMapsOwn_areTheMapTabs()
    {
        (MapTabText.Texts(MapText.Map), MapTabText.Texts(MapText.DrainList)).ShouldBe((typeof(MapTab), typeof(DrainList)));
    }

    [Fact]
    public void Texts_textsItHasNoArmFor_areRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapTabText.Texts((MapText)99));
    }
}
