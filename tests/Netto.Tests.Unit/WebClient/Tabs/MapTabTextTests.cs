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
            .Where(label => Texts(label.Text).Get(label.Key) is not { } text || (label.Args.Count > 0 && !text.Contains("{0}", StringComparison.Ordinal)))
            .Select(label => $"{label.Text} {label.Key}").ShouldBeEmpty();
    }

    // Each kind of text a link is worded by, the component whose .resx holds it.
    [Fact]
    public void Texts_everyKind_isAComponentsTexts()
    {
        Enum.GetValues<MapText>().Select(text => Texts(text).Keys.Count > 0).ShouldAllBe(any => any);
    }

    private static ResxText Texts(MapText text) => text switch
    {
        MapText.Map => Resources,
        MapText.DrainList => ResxText.For<DrainList>(),
        MapText.SplitCard => ResxText.For<SplitCard>(),
        MapText.CarCard => ResxText.For<CarCard>(),
        MapText.VoucherCard => ResxText.For<VoucherCard>(),
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, null),
    };
}
