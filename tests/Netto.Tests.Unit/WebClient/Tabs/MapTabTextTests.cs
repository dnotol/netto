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
        RiverModule.Household => typeof(HouseholdCard),
        RiverModule.CorporateTax => typeof(CorporateTaxCard),
        RiverModule.Dividend => typeof(DividendCard),
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null),
    };

    // A module on the map is named as its card is.
    [Theory]
    [MemberData(nameof(Modules))]
    public void MapTabResx_aModule_isNamedByItsCardsTitle(RiverModule module)
    {
        Resources.Get(MapTabText.Module(module)).ShouldBe(ResxText.For(Card(module)).Get("Title"));
    }

    public static TheoryData<MapOwner> Owners => new(Enum.GetValues<MapOwner>());

    // Each column of the map is labelled by whose money it holds.
    [Theory]
    [MemberData(nameof(Owners))]
    public void MapTabResx_aColumn_hasItsLabel(MapOwner owner)
    {
        Resources.Get(MapTabText.Column(owner)).ShouldNotBeNullOrWhiteSpace();
    }

    // The sentences of the conditions are on no surface of the map: the cards say them with their figures
    // (the Corporate tax, Copyright royalty, Split and Dividend cards), so their texts are the cards' alone.
    [Fact]
    public void MapTabResx_theSentencesOfTheConditions_areGone()
    {
        Resources.Keys.Where(key => key.StartsWith("Condition_", StringComparison.Ordinal)).ShouldBeEmpty();
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
        { MapTabText.BenefitsBadge(holds: false), [], "benefits lost" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false)), [], "royalty lowers" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(40_000m, null, false)), [], "royalty lowers" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false)), [], "royalty: none" },
        { MapTabText.RoyaltyBadge(new RoyaltyLowersPayKeepingReducedRate(null, 50_000m, false)), [], "no pay keeps" },
        { MapTabText.ReserveBadge(holds: true), [], "reserve met" },
        { MapTabText.ReserveBadge(holds: false), [], "reserve short" },
        { MapTabText.TaxBaseBadge, ["€9,999,999.99"], "+€9,999,999.99 to tax base" },
    };

    [Theory]
    [MemberData(nameof(BadgeWords))]
    public void MapTabResx_aBadge_isWordedByTheKeyItsHelperBuildsInThirtyCharactersAtMost(string key, string[] args, string words)
    {
        var text = Resources.Format(key, args);

        (text, text.Length <= 30).ShouldBe((words, true));
    }

    // None of the texts of the list of a module's links, which the map once had beside it, is here any more.
    [Fact]
    public void MapTabResx_theTextsOfTheListOfAModulesLinks_areGone()
    {
        Resources.Keys.Intersect(["Link", "LinkWithoutAmount", "LinksOf", "LinksOfNone", "ChooseHint", "NoLinks", "NoFigures", "Hidden_One", "Hidden_Many"]).ShouldBeEmpty();
    }

    // The line over the chosen module's card says what it receives, each amount and where it comes from.
    [Fact]
    public void MapTabResx_theLineOfWhatAModuleReceives_saysEachAmountAndWhereItComesFrom()
    {
        (Resources.Get("Receives"), Resources.Format("ReceivedFrom", ["€1,000.00"]), Resources.Get("ReceivedBetween")).ShouldBe(("Receives", "€1,000.00 from", "·"));
    }

    // What a stretch of the pocket line carries: what has reached the director so far.
    [Fact]
    public void MapTabResx_aStretchOfThePocketLine_carriesWhatHasReachedYouSoFar()
    {
        Resources.Get(MapTabText.PocketSoFar).ShouldBe("What has reached you so far");
    }

    // Every link of these plans is worded by the texts its label names, its arguments taken, the pocket line's
    // stretches among them.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Carries_everyLinkOfAPlan_isWordedByItsTexts(bool companyPays)
    {
        var river = companyPays ? MapLinksTests.Full.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.035m) : MapLinksTests.Full;

        MapPocketLine.Of(MapLinks.Of(river, river.Run())).Select(link => link.Carries)

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

    // The lever cards whose « The company pays » words a lever's share of the company's line.
    [Fact]
    public void Texts_theLeverCardsOfAShare_areTheirCards()
    {
        (MapTabText.Texts(MapText.RentCard), MapTabText.Texts(MapText.AllowanceCard)).ShouldBe((typeof(RentCard), typeof(AllowanceCard)));
    }

    [Fact]
    public void Texts_textsItHasNoArmFor_areRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapTabText.Texts((MapText)99));
    }
}
