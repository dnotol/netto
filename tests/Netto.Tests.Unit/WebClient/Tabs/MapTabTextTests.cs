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
        (Resources.Get("Eyebrow"), Resources.Get("Title")).ShouldBe(("Your plan from above", "How your modules act on each other"));
    }

    // A module on the map is named as its card is.
    [Theory]
    [MemberData(nameof(Modules))]
    public void MapTabResx_aModule_isNamedByItsCardsTitle(RiverModule module)
    {
        Resources.Get(MapTabText.Module(module)).ShouldBe(ResxText.For(Card(module)).Get("Title"));
    }

    [Fact]
    public void MapTabResx_aLink_saysWhatItCarriesItsAmountAndWhereFromAndTo()
    {
        (Resources.Format(MapTabText.Link, "Net pay", "€40,000.00", "Director pay", "Your pocket"),
                Resources.Format(MapTabText.LinkWithoutAmount, "The pay keeps the reduced rate", "The split", "Corporate tax"))
            .ShouldBe(("Net pay: €40,000.00, from Director pay to Your pocket", "The pay keeps the reduced rate, from The split to Corporate tax"));
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

    [Fact]
    public void MapTabResx_theRiversEdge_isOutOfTheRiver()
    {
        Resources.Get(MapTabText.OutOfRiver).ShouldBe("out of the river");
    }

    [Fact]
    public void MapTabResx_theListsHeading_namesTheModuleChosen()
    {
        (Resources.Format(MapTabText.LinksOf, "Company car"), Resources.Get(MapTabText.LinksOfNone)).ShouldBe(("Links of Company car", "A module's links"));
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
