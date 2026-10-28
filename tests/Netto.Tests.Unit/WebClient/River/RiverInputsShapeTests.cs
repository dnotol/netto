using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Saved plans outlive the code (PlanStorage). When this test fails, the river's inputs changed
// shape: give PlanStorage the new input, raise PlanStorage.Version, write the patch that brings a plan
// saved by the version before to it (the new member as frozen JSON with its own origin, or the plan
// refused), add the new version's document to StoredPlanDocuments, then update the shape here.
// The exceptions so far: the car's engine, CO2, order date and statuses joined version 2 without
// raising it, since version 2 had not left the car's story branch (operator, 2026-10-22); the rest of
// the copyright royalty's story, its attestation du travail des arts and its creative share, joins
// version 3 the same way (decided unattended, 2026-10-22). Whether the royalty follows the pay raised
// version 5 (2026-10-23), what the same car would cost privately version 6 (2026-10-23), and the band
// of INASTI's company contribution version 7 (2026-10-25), which the rate of the fund's management fee
// joined without raising it, since version 7 had not left its story branch (decided unattended,
// 2026-10-25). The partner raised version 8 (2026-10-28).
public class RiverInputsShapeTests
{
    [Theory]
    [InlineData(typeof(RiverInputs), "IncomeYear:Int32 Company:CompanyFigures Composition:PlanComposition Vouchers:VoucherInputs Allowances:AllowanceInputs Rent:RentInputs Car:CarInputs Copyright:CopyrightInputs SmallCompany:Input`1[Boolean] FinancialYear:Input`1[FinancialYear] CompanyContributionBand:Input`1[CompanyContributionBand] AdvancePayments:AdvancePaymentInputs DirectorPay:Input`1[Decimal] Municipality:MunicipalityChoice TaxPrepayments:TaxPrepaymentInputs Reserve:Input`1[Decimal] DividendCategory:Input`1[WithholdingCategory] EarlierProfits:Input`1[Decimal] ContributionsPaidBy:Input`1[ContributionsPaidBy] BilledContributions:Input`1[Nullable`1[Decimal]] FundFeeRate:Input`1[Nullable`1[Decimal]] Household:HouseholdInputs")]
    [InlineData(typeof(CompanyFigures), "Revenue:Input`1[Decimal] RunningCosts:Input`1[Decimal]")]
    [InlineData(typeof(PlanComposition), "Vouchers:Boolean Allowances:Boolean Rent:Boolean Car:Boolean Copyright:Boolean Household:Boolean")]
    [InlineData(typeof(CopyrightInputs), "Royalty:Input`1[Decimal] FollowsPay:Input`1[Boolean] AverageWithinCap:Input`1[Boolean] HoldsAttestation:Input`1[Boolean] CreativeShare:Input`1[Nullable`1[Decimal]]")]
    [InlineData(typeof(VoucherInputs), "MealVoucherDays:Input`1[Int32] EcoVouchers:Input`1[Decimal] SportCultureVouchers:Input`1[Decimal]")]
    [InlineData(typeof(AllowanceInputs), "Office:Input`1[Decimal] Internet:Input`1[Decimal] Computer:Input`1[Decimal] Phone:Input`1[Decimal]")]
    [InlineData(typeof(RentInputs), "CadastralIncome:Input`1[Decimal] ShareLet:Input`1[Decimal] Rent:Input`1[Decimal]")]
    [InlineData(typeof(CarInputs), "CatalogueValue:Input`1[Decimal] FirstRegistration:Input`1[DateOnly] YearlyCosts:Input`1[Decimal] CompanyPaysPrivateEnergy:Input`1[Boolean] PersonalContribution:Input`1[Decimal] Engine:Input`1[CarEngine] Co2:Input`1[Int32] NoCo2Data:Input`1[Boolean] Acquired:Input`1[DateOnly] FakeHybrid:Input`1[Boolean] MatchingCarCo2:Input`1[Int32] NoMatchingCar:Input`1[Boolean] SmallNaturalGas:Input`1[Boolean] PrivateCost:Input`1[Nullable`1[Decimal]]")]
    [InlineData(typeof(AdvancePaymentInputs), "First:Input`1[Decimal] Second:Input`1[Decimal] Third:Input`1[Decimal] Fourth:Input`1[Decimal]")]
    [InlineData(typeof(TaxPrepaymentInputs), "Withheld:Input`1[Nullable`1[Decimal]] Paid:AdvancePaymentInputs Starter:Input`1[Boolean]")]
    [InlineData(typeof(HouseholdInputs), "PartnerIncome:Input`1[Decimal] BothAged66:Input`1[Boolean] TaxedSeparately:Input`1[Boolean]")]
    [InlineData(typeof(MunicipalityChoice), "Rates:MunicipalRates Municipality:Municipality Rate:Decimal Origin:InputOrigin")]
    public void Constructor_savedPlansKnowIt_isTheShapePlanStorageStores(Type type, string shape)
    {
        var parameters = Parameters(type);

        string.Join(' ', parameters.Select(parameter => $"{parameter.Name}:{Name(parameter.ParameterType)}")).ShouldBe(shape);
    }

    // A record's positional members in their order: the parameters of its one public constructor, since a
    // record's copy constructor is never public. ExampleOriginsTests walks the river's inputs by them.
    internal static IReadOnlyList<System.Reflection.ParameterInfo> Parameters(Type type) => type.GetConstructors().Single().GetParameters();

    private static string Name(Type type) => type.IsGenericType
        ? $"{type.Name}[{string.Join(',', type.GetGenericArguments().Select(Name))}]"
        : type.Name;
}
