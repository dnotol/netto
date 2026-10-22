using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Netto.Core.CompanyCar;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.SocialContributions;
using Netto.Web.Client.DirectorPay;

namespace Netto.Web.Client.River;

// A plan the user saved: its name and the river's inputs as they were, or null when what was stored
// cannot be read back as this version of Netto reads a plan (Stored keeps it as it was, so it is
// written back untouched rather than lost).
internal sealed record SavedPlan(string Id, string Name, RiverInputs? Inputs, JsonElement Stored)
{
    public bool Readable => Inputs is not null;
}

// Every saved plan, the one starred « Today », the user's current setup, and the one the river was
// last opened from.
internal sealed record PlanBook(IReadOnlyList<SavedPlan> Plans, string? Today, string? Opened)
{
    public static PlanBook Empty { get; } = new([], null, null);
}

// How saved plans are kept in the browser: one JSON document under Key, carrying the version of its
// format. A plan is stored as plain values with their origins, never as RiverInputs itself, so the
// format changes only when this file does: a new input of the river is a new field here and a new
// Version (RiverInputsShapeTests fails until it is). Reading is strict: a missing field, a field
// this version does not know, an unknown option or municipality makes that plan unreadable, never
// a plan with a default in place of what was saved. A document that cannot be read at all, or comes
// from a newer version, is refused whole. A document of an older version is read in its own format,
// each plan as it was saved, and is written back in this one: version 1 held no company car, so its
// plans are read as plans without one (decided 2026-10-22), and versions 1 and 2 no copyright royalty,
// so theirs are read without one. The car's engine, CO2, order date and statuses joined version 2
// without raising it, since version 2 had not left the car's story branch; a plan saved by the car's
// first build does not open (operator, 2026-10-22). The rest of the royalty's story joins version 3
// the same way (decided unattended, 2026-10-22).
internal static class PlanStorage
{
    public const string Key = "netto.plans";

    // Where a document that cannot be read is kept aside before anything is written over it.
    public const string UnreadableKey = "netto.plans.unreadable";

    // 2: the company car. 3: the copyright royalty.
    public const int Version = 3;

    private const int Version1 = 1;

    private const int Version2 = 2;

    // The plans stored, or Unreadable when the document is there but cannot be read.
    public static (PlanBook Book, bool Unreadable) Read(string? json)
    {
        if (json is null)
            return (PlanBook.Empty, false);

        StoredBook? stored;
        try
        {
            stored = JsonSerializer.Deserialize(json, PlanJson.Default.StoredBook);
        }
        catch (JsonException)
        {
            return (PlanBook.Empty, true);
        }

        if (stored is null || stored.Version is not (Version or Version2 or Version1))
            return (PlanBook.Empty, true);

        var plans = stored.Plans.Select(plan => stored.Version switch
        {
            Version => Plan(plan),
            Version2 => PlanOfOlderVersion(plan, PlanJson.Default.StoredInputsVersion2, FromVersion2),
            _ => PlanOfOlderVersion(plan, PlanJson.Default.StoredInputsVersion1, FromVersion1),
        }).ToList();
        return (new PlanBook(plans, stored.Today, stored.Opened), false);
    }

    public static string Write(PlanBook book) => JsonSerializer.Serialize(
        new StoredBook(Version, book.Today, book.Opened, [.. book.Plans.Select(plan => new StoredPlan(plan.Id, plan.Name, plan.Stored))]),
        PlanJson.Default.StoredBook);

    // A plan as it is stored, for a new plan.
    public static JsonElement Store(RiverInputs inputs) => JsonSerializer.SerializeToElement(Stored(inputs), PlanJson.Default.StoredInputs);

    private static SavedPlan Plan(StoredPlan plan) => new(plan.Id, plan.Name, Inputs(plan.Inputs, PlanJson.Default.StoredInputs, stored => stored), plan.Inputs.Clone());

    // Read as an older version wrote it and kept in this version's format, so the book is written
    // back in one version; a plan it cannot read is kept as it was stored.
    private static SavedPlan PlanOfOlderVersion<T>(StoredPlan plan, JsonTypeInfo<T> format, Func<T, StoredInputs> current) =>
        Inputs(plan.Inputs, format, current) is { } inputs
            ? new SavedPlan(plan.Id, plan.Name, inputs, Store(inputs))
            : new SavedPlan(plan.Id, plan.Name, null, plan.Inputs.Clone());

    private static RiverInputs? Inputs<T>(JsonElement stored, JsonTypeInfo<T> format, Func<T, StoredInputs> current)
    {
        try
        {
            return JsonSerializer.Deserialize(stored, format) is { } inputs ? Inputs(current(inputs)) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // A plan of version 1, which held no car and no royalty: out of the plan, with the example's values.
    private static StoredInputs FromVersion1(StoredInputsVersion1 stored) => new(
        stored.IncomeYear,
        stored.Company,
        new StoredComposition(stored.Composition.Vouchers, stored.Composition.Allowances, stored.Composition.Rent, false, false),
        stored.Vouchers,
        stored.Allowances,
        stored.Rent,
        Stored(RiverInputs.Example.Car),
        Stored(RiverInputs.Example.Copyright),
        stored.SmallCompany,
        stored.FinancialYear,
        stored.AdvancePayments,
        stored.DirectorPay,
        stored.Municipality,
        stored.TaxPrepayments,
        stored.Reserve,
        stored.DividendCategory,
        stored.EarlierProfits,
        stored.ContributionsPaidBy);

    // A plan of version 2, which held no royalty: out of the plan, with the example's values.
    private static StoredInputs FromVersion2(StoredInputsVersion2 stored) => new(
        stored.IncomeYear,
        stored.Company,
        new StoredComposition(stored.Composition.Vouchers, stored.Composition.Allowances, stored.Composition.Rent, stored.Composition.Car, false),
        stored.Vouchers,
        stored.Allowances,
        stored.Rent,
        stored.Car,
        Stored(RiverInputs.Example.Copyright),
        stored.SmallCompany,
        stored.FinancialYear,
        stored.AdvancePayments,
        stored.DirectorPay,
        stored.Municipality,
        stored.TaxPrepayments,
        stored.Reserve,
        stored.DividendCategory,
        stored.EarlierProfits,
        stored.ContributionsPaidBy);

    private static StoredInputs Stored(RiverInputs inputs) => new(
        inputs.IncomeYear,
        inputs.Company is { } company ? new StoredCompany(Stored(company.Revenue), Stored(company.RunningCosts)) : null,
        new StoredComposition(inputs.Composition.Vouchers, inputs.Composition.Allowances, inputs.Composition.Rent, inputs.Composition.Car, inputs.Composition.Copyright),
        new StoredVouchers(Stored(inputs.Vouchers.MealVoucherDays), Stored(inputs.Vouchers.EcoVouchers), Stored(inputs.Vouchers.SportCultureVouchers)),
        new StoredAllowances(Stored(inputs.Allowances.Office), Stored(inputs.Allowances.Internet), Stored(inputs.Allowances.Computer), Stored(inputs.Allowances.Phone)),
        new StoredRent(Stored(inputs.Rent.CadastralIncome), Stored(inputs.Rent.ShareLet), Stored(inputs.Rent.Rent)),
        Stored(inputs.Car),
        Stored(inputs.Copyright),
        Stored(inputs.SmallCompany),
        Stored(inputs.FinancialYear),
        Stored(inputs.AdvancePayments),
        Stored(inputs.DirectorPay),
        new StoredMunicipality(inputs.Municipality.Municipality?.InsCode, inputs.Municipality.Rate, inputs.Municipality.Origin),
        new StoredTaxPrepayments(Stored(inputs.TaxPrepayments.Withheld), Stored(inputs.TaxPrepayments.Paid), Stored(inputs.TaxPrepayments.Starter)),
        Stored(inputs.Reserve),
        Stored(inputs.DividendCategory),
        Stored(inputs.EarlierProfits),
        Stored(inputs.ContributionsPaidBy));

    // Null when the year has no municipal rates or no longer lists the municipality saved: a plan is
    // never opened with another rate than the one it was saved with.
    private static RiverInputs? Inputs(StoredInputs stored)
    {
        if (MunicipalRates.For(stored.IncomeYear) is not { } rates)
            return null;

        Municipality? municipality = null;
        if (stored.Municipality.InsCode is { } code && (municipality = rates.Municipalities.FirstOrDefault(listed => listed.InsCode == code)) is null)
            return null;

        return new RiverInputs(
            stored.IncomeYear,
            stored.Company is { } company ? new CompanyFigures(Input(company.Revenue), Input(company.RunningCosts)) : null,
            new PlanComposition(stored.Composition.Vouchers, stored.Composition.Allowances, stored.Composition.Rent, stored.Composition.Car, stored.Composition.Copyright),
            new VoucherInputs(Input(stored.Vouchers.MealVoucherDays), Input(stored.Vouchers.EcoVouchers), Input(stored.Vouchers.SportCultureVouchers)),
            new AllowanceInputs(Input(stored.Allowances.Office), Input(stored.Allowances.Internet), Input(stored.Allowances.Computer), Input(stored.Allowances.Phone)),
            new RentInputs(Input(stored.Rent.CadastralIncome), Input(stored.Rent.ShareLet), Input(stored.Rent.Rent)),
            Input(stored.Car),
            new CopyrightInputs(
                Input(stored.Copyright.Royalty), Input(stored.Copyright.AverageWithinCap), Input(stored.Copyright.HoldsAttestation), Input(stored.Copyright.CreativeShare)),
            Input(stored.SmallCompany),
            Input(stored.FinancialYear),
            Input(stored.AdvancePayments),
            Input(stored.DirectorPay),
            new MunicipalityChoice(rates, municipality, stored.Municipality.Rate, stored.Municipality.Origin),
            new TaxPrepaymentInputs(Input(stored.TaxPrepayments.Withheld), Input(stored.TaxPrepayments.Paid), Input(stored.TaxPrepayments.Starter)),
            Input(stored.Reserve),
            Input(stored.DividendCategory),
            Input(stored.EarlierProfits),
            Input(stored.ContributionsPaidBy));
    }

    private static StoredValue<T> Stored<T>(Input<T> input) => new(input.Value, input.Origin);

    private static Input<T> Input<T>(StoredValue<T> stored) => new(stored.Value, stored.Origin);

    private static StoredCar Stored(CarInputs car) => new(
        Stored(car.CatalogueValue), Stored(car.FirstRegistration), Stored(car.YearlyCosts), Stored(car.CompanyPaysPrivateEnergy), Stored(car.PersonalContribution),
        Stored(car.Engine), Stored(car.Co2), Stored(car.NoCo2Data), Stored(car.Acquired), Stored(car.FakeHybrid), Stored(car.MatchingCarCo2),
        Stored(car.NoMatchingCar), Stored(car.SmallNaturalGas));

    private static CarInputs Input(StoredCar stored) => new(
        Input(stored.CatalogueValue), Input(stored.FirstRegistration), Input(stored.YearlyCosts), Input(stored.CompanyPaysPrivateEnergy), Input(stored.PersonalContribution),
        Input(stored.Engine), Input(stored.Co2), Input(stored.NoCo2Data), Input(stored.Acquired), Input(stored.FakeHybrid), Input(stored.MatchingCarCo2),
        Input(stored.NoMatchingCar), Input(stored.SmallNaturalGas));

    private static StoredCopyright Stored(CopyrightInputs copyright) =>
        new(Stored(copyright.Royalty), Stored(copyright.AverageWithinCap), Stored(copyright.HoldsAttestation), Stored(copyright.CreativeShare));

    private static StoredPayments Stored(AdvancePaymentInputs payments) =>
        new(Stored(payments.First), Stored(payments.Second), Stored(payments.Third), Stored(payments.Fourth));

    private static AdvancePaymentInputs Input(StoredPayments stored) =>
        new(Input(stored.First), Input(stored.Second), Input(stored.Third), Input(stored.Fourth));
}

// The stored document and plan, version 3. Renaming or removing a member changes the format.
internal sealed record StoredBook(int Version, string? Today, string? Opened, IReadOnlyList<StoredPlan> Plans);

internal sealed record StoredPlan(string Id, string Name, JsonElement Inputs);

internal sealed record StoredInputs(
    int IncomeYear,
    StoredCompany? Company,
    StoredComposition Composition,
    StoredVouchers Vouchers,
    StoredAllowances Allowances,
    StoredRent Rent,
    StoredCar Car,
    StoredCopyright Copyright,
    StoredValue<bool> SmallCompany,
    StoredValue<FinancialYear> FinancialYear,
    StoredPayments AdvancePayments,
    StoredValue<decimal> DirectorPay,
    StoredMunicipality Municipality,
    StoredTaxPrepayments TaxPrepayments,
    StoredValue<decimal> Reserve,
    StoredValue<WithholdingCategory> DividendCategory,
    StoredValue<decimal> EarlierProfits,
    StoredValue<ContributionsPaidBy> ContributionsPaidBy);

// A plan as version 2 stored it: no royalty, and a composition of four optional modules.
internal sealed record StoredInputsVersion2(
    int IncomeYear,
    StoredCompany? Company,
    StoredCompositionVersion2 Composition,
    StoredVouchers Vouchers,
    StoredAllowances Allowances,
    StoredRent Rent,
    StoredCar Car,
    StoredValue<bool> SmallCompany,
    StoredValue<FinancialYear> FinancialYear,
    StoredPayments AdvancePayments,
    StoredValue<decimal> DirectorPay,
    StoredMunicipality Municipality,
    StoredTaxPrepayments TaxPrepayments,
    StoredValue<decimal> Reserve,
    StoredValue<WithholdingCategory> DividendCategory,
    StoredValue<decimal> EarlierProfits,
    StoredValue<ContributionsPaidBy> ContributionsPaidBy);

// A plan as version 1 stored it: no car, and a composition of three optional modules.
internal sealed record StoredInputsVersion1(
    int IncomeYear,
    StoredCompany? Company,
    StoredCompositionVersion1 Composition,
    StoredVouchers Vouchers,
    StoredAllowances Allowances,
    StoredRent Rent,
    StoredValue<bool> SmallCompany,
    StoredValue<FinancialYear> FinancialYear,
    StoredPayments AdvancePayments,
    StoredValue<decimal> DirectorPay,
    StoredMunicipality Municipality,
    StoredTaxPrepayments TaxPrepayments,
    StoredValue<decimal> Reserve,
    StoredValue<WithholdingCategory> DividendCategory,
    StoredValue<decimal> EarlierProfits,
    StoredValue<ContributionsPaidBy> ContributionsPaidBy);

internal sealed record StoredValue<T>(T Value, InputOrigin Origin);

internal sealed record StoredCompany(StoredValue<decimal> Revenue, StoredValue<decimal> RunningCosts);

internal sealed record StoredComposition(bool Vouchers, bool Allowances, bool Rent, bool Car, bool Copyright);

internal sealed record StoredCompositionVersion2(bool Vouchers, bool Allowances, bool Rent, bool Car);

internal sealed record StoredCompositionVersion1(bool Vouchers, bool Allowances, bool Rent);

internal sealed record StoredCopyright(
    StoredValue<decimal> Royalty, StoredValue<bool> AverageWithinCap, StoredValue<bool> HoldsAttestation, StoredValue<decimal?> CreativeShare);

internal sealed record StoredVouchers(StoredValue<int> MealVoucherDays, StoredValue<decimal> EcoVouchers, StoredValue<decimal> SportCultureVouchers);

internal sealed record StoredAllowances(StoredValue<decimal> Office, StoredValue<decimal> Internet, StoredValue<decimal> Computer, StoredValue<decimal> Phone);

internal sealed record StoredRent(StoredValue<decimal> CadastralIncome, StoredValue<decimal> ShareLet, StoredValue<decimal> Rent);

internal sealed record StoredCar(
    StoredValue<decimal> CatalogueValue,
    StoredValue<DateOnly> FirstRegistration,
    StoredValue<decimal> YearlyCosts,
    StoredValue<bool> CompanyPaysPrivateEnergy,
    StoredValue<decimal> PersonalContribution,
    StoredValue<CarEngine> Engine,
    StoredValue<int> Co2,
    StoredValue<bool> NoCo2Data,
    StoredValue<DateOnly> Acquired,
    StoredValue<bool> FakeHybrid,
    StoredValue<int> MatchingCarCo2,
    StoredValue<bool> NoMatchingCar,
    StoredValue<bool> SmallNaturalGas);

internal sealed record StoredPayments(StoredValue<decimal> First, StoredValue<decimal> Second, StoredValue<decimal> Third, StoredValue<decimal> Fourth);

// The municipality by its INS code, or none, with the rate on screen and where it comes from.
internal sealed record StoredMunicipality(int? InsCode, decimal Rate, InputOrigin Origin);

internal sealed record StoredTaxPrepayments(StoredValue<decimal?> Withheld, StoredPayments Paid, StoredValue<bool> Starter);

// Strict, so a plan is read as it was saved or not at all: every field required, none unknown, no
// null where the format has none, options by name so reordering an enum changes nothing. Generated
// at build time, so trimming the WebAssembly app cannot strip it.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectRequiredConstructorParameters = true,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(StoredBook))]
[JsonSerializable(typeof(StoredInputs))]
[JsonSerializable(typeof(StoredInputsVersion2))]
[JsonSerializable(typeof(StoredInputsVersion1))]
internal sealed partial class PlanJson : JsonSerializerContext;
