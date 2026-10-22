using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Saved plans outlive the code (PlanStorage). When this test fails, the river's inputs changed
// shape: give PlanStorage the new input, raise PlanStorage.Version, decide how a plan saved by the
// version before reads (refused, or read with the new input's own origin), then update the shape here.
// The exceptions so far: the car's engine, CO2, order date and statuses joined version 2 without
// raising it, since version 2 had not left the car's story branch (operator, 2026-10-22); the rest of
// the copyright royalty's story joins version 3 the same way (decided unattended, 2026-10-22).
public class RiverInputsShapeTests
{
    [Theory]
    [InlineData(typeof(RiverInputs), "IncomeYear:Int32 Company:CompanyFigures Composition:PlanComposition Vouchers:VoucherInputs Allowances:AllowanceInputs Rent:RentInputs Car:CarInputs Copyright:CopyrightInputs SmallCompany:Input`1[Boolean] FinancialYear:Input`1[FinancialYear] AdvancePayments:AdvancePaymentInputs DirectorPay:Input`1[Decimal] Municipality:MunicipalityChoice TaxPrepayments:TaxPrepaymentInputs Reserve:Input`1[Decimal] DividendCategory:Input`1[WithholdingCategory] EarlierProfits:Input`1[Decimal] ContributionsPaidBy:Input`1[ContributionsPaidBy]")]
    [InlineData(typeof(CompanyFigures), "Revenue:Input`1[Decimal] RunningCosts:Input`1[Decimal]")]
    [InlineData(typeof(PlanComposition), "Vouchers:Boolean Allowances:Boolean Rent:Boolean Car:Boolean Copyright:Boolean")]
    [InlineData(typeof(CopyrightInputs), "Royalty:Input`1[Decimal] AverageWithinCap:Input`1[Boolean]")]
    [InlineData(typeof(VoucherInputs), "MealVoucherDays:Input`1[Int32] EcoVouchers:Input`1[Decimal] SportCultureVouchers:Input`1[Decimal]")]
    [InlineData(typeof(AllowanceInputs), "Office:Input`1[Decimal] Internet:Input`1[Decimal] Computer:Input`1[Decimal] Phone:Input`1[Decimal]")]
    [InlineData(typeof(RentInputs), "CadastralIncome:Input`1[Decimal] ShareLet:Input`1[Decimal] Rent:Input`1[Decimal]")]
    [InlineData(typeof(CarInputs), "CatalogueValue:Input`1[Decimal] FirstRegistration:Input`1[DateOnly] YearlyCosts:Input`1[Decimal] CompanyPaysPrivateEnergy:Input`1[Boolean] PersonalContribution:Input`1[Decimal] Engine:Input`1[CarEngine] Co2:Input`1[Int32] NoCo2Data:Input`1[Boolean] Acquired:Input`1[DateOnly] FakeHybrid:Input`1[Boolean] MatchingCarCo2:Input`1[Int32] NoMatchingCar:Input`1[Boolean] SmallNaturalGas:Input`1[Boolean]")]
    [InlineData(typeof(AdvancePaymentInputs), "First:Input`1[Decimal] Second:Input`1[Decimal] Third:Input`1[Decimal] Fourth:Input`1[Decimal]")]
    [InlineData(typeof(TaxPrepaymentInputs), "Withheld:Input`1[Nullable`1[Decimal]] Paid:AdvancePaymentInputs Starter:Input`1[Boolean]")]
    [InlineData(typeof(MunicipalityChoice), "Rates:MunicipalRates Municipality:Municipality Rate:Decimal Origin:InputOrigin")]
    public void Constructor_savedPlansKnowIt_isTheShapePlanStorageStores(Type type, string shape)
    {
        var parameters = type.GetConstructors().Single(constructor => constructor.GetParameters().Length > 0 && !IsCopyConstructor(type, constructor)).GetParameters();

        string.Join(' ', parameters.Select(parameter => $"{parameter.Name}:{Name(parameter.ParameterType)}")).ShouldBe(shape);
    }

    private static bool IsCopyConstructor(Type type, System.Reflection.ConstructorInfo constructor) =>
        constructor.GetParameters() is [var only] && only.ParameterType == type;

    private static string Name(Type type) => type.IsGenericType
        ? $"{type.Name}[{string.Join(',', type.GetGenericArguments().Select(Name))}]"
        : type.Name;
}
