using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.Steps;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The company car's benefit in kind has two amounts, before the director's contribution and after it,
// and the one taxed as pay is the second (CarResult.Benefit). Each card, and the car's steps in their
// details, names which one it shows, and Director pay names the one it holds as the car card does.
public class BenefitInKindNamesTests
{
    private static readonly ResxText DirectorPay = ResxText.For<DirectorPayCard>();

    private static readonly ResxText Car = ResxText.For<CarCard>();

    private static readonly ResxText Steps = ResxText.For<StepDetails>();

    [Fact]
    public void DirectorPay_benefitInKind_isNamedAsTheCarCardsBenefitTaxedAsPay()
    {
        DirectorPay.Get("Figure_BenefitInKind").ShouldBe(Car.Get(CarFigures.BenefitTaxed));
    }

    [Fact]
    public void CarCard_benefitTaxedAsPay_isTheCarsAfterTheContribution()
    {
        Car.Get(CarFigures.BenefitTaxed).ShouldBe("Car's benefit in kind after your contribution");
    }

    [Fact]
    public void CarCard_benefitOnTheScale_isTheCarsBeforeTheContribution()
    {
        Car.Get("Figure_Benefit").ShouldBe("Car's benefit in kind before your contribution");
    }

    // The steps the two lines are the amounts of read as the lines do.
    [Fact]
    public void CarSteps_areNamedAsTheCarCardsLines()
    {
        (Steps.Get("Title_car_benefit"), Steps.Get("Title_car_benefit_taxed"))
            .ShouldBe((Car.Get("Figure_Benefit"), Car.Get(CarFigures.BenefitTaxed)));
    }

    // Its bubble says the contribution is off it, which the line's name says too.
    [Fact]
    public void DirectorPay_benefitInKindsBubble_saysTheContributionIsOffIt()
    {
        DirectorPay.Get("Help_BenefitInKind").ShouldNotBeNull().ShouldContain("less your contribution");
    }
}
