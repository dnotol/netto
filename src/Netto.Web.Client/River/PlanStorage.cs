using System.Text.Json;
using System.Text.Json.Serialization;
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
// from a newer version, is refused whole.
internal static class PlanStorage
{
    public const string Key = "netto.plans";

    // Where a document that cannot be read is kept aside before anything is written over it.
    public const string UnreadableKey = "netto.plans.unreadable";

    public const int Version = 1;

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

        if (stored is null || stored.Version != Version)
            return (PlanBook.Empty, true);

        var plans = stored.Plans.Select(plan => new SavedPlan(plan.Id, plan.Name, Inputs(plan.Inputs), plan.Inputs.Clone())).ToList();
        return (new PlanBook(plans, stored.Today, stored.Opened), false);
    }

    public static string Write(PlanBook book) => JsonSerializer.Serialize(
        new StoredBook(Version, book.Today, book.Opened, [.. book.Plans.Select(plan => new StoredPlan(plan.Id, plan.Name, plan.Stored))]),
        PlanJson.Default.StoredBook);

    // A plan as it is stored, for a new plan.
    public static JsonElement Store(RiverInputs inputs) => JsonSerializer.SerializeToElement(Stored(inputs), PlanJson.Default.StoredInputs);

    private static RiverInputs? Inputs(JsonElement stored)
    {
        try
        {
            return JsonSerializer.Deserialize(stored, PlanJson.Default.StoredInputs) is { } inputs ? Inputs(inputs) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static StoredInputs Stored(RiverInputs inputs) => new(
        inputs.IncomeYear,
        inputs.Company is { } company ? new StoredCompany(Stored(company.Revenue), Stored(company.RunningCosts)) : null,
        new StoredComposition(inputs.Composition.Vouchers, inputs.Composition.Allowances, inputs.Composition.Rent),
        new StoredVouchers(Stored(inputs.Vouchers.MealVoucherDays), Stored(inputs.Vouchers.EcoVouchers), Stored(inputs.Vouchers.SportCultureVouchers)),
        new StoredAllowances(Stored(inputs.Allowances.Office), Stored(inputs.Allowances.Internet), Stored(inputs.Allowances.Computer), Stored(inputs.Allowances.Phone)),
        new StoredRent(Stored(inputs.Rent.CadastralIncome), Stored(inputs.Rent.ShareLet), Stored(inputs.Rent.Rent)),
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
            new PlanComposition(stored.Composition.Vouchers, stored.Composition.Allowances, stored.Composition.Rent),
            new VoucherInputs(Input(stored.Vouchers.MealVoucherDays), Input(stored.Vouchers.EcoVouchers), Input(stored.Vouchers.SportCultureVouchers)),
            new AllowanceInputs(Input(stored.Allowances.Office), Input(stored.Allowances.Internet), Input(stored.Allowances.Computer), Input(stored.Allowances.Phone)),
            new RentInputs(Input(stored.Rent.CadastralIncome), Input(stored.Rent.ShareLet), Input(stored.Rent.Rent)),
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

    private static StoredPayments Stored(AdvancePaymentInputs payments) =>
        new(Stored(payments.First), Stored(payments.Second), Stored(payments.Third), Stored(payments.Fourth));

    private static AdvancePaymentInputs Input(StoredPayments stored) =>
        new(Input(stored.First), Input(stored.Second), Input(stored.Third), Input(stored.Fourth));
}

// The stored document and plan, version 1. Renaming or removing a member changes the format.
internal sealed record StoredBook(int Version, string? Today, string? Opened, IReadOnlyList<StoredPlan> Plans);

internal sealed record StoredPlan(string Id, string Name, JsonElement Inputs);

internal sealed record StoredInputs(
    int IncomeYear,
    StoredCompany? Company,
    StoredComposition Composition,
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

internal sealed record StoredComposition(bool Vouchers, bool Allowances, bool Rent);

internal sealed record StoredVouchers(StoredValue<int> MealVoucherDays, StoredValue<decimal> EcoVouchers, StoredValue<decimal> SportCultureVouchers);

internal sealed record StoredAllowances(StoredValue<decimal> Office, StoredValue<decimal> Internet, StoredValue<decimal> Computer, StoredValue<decimal> Phone);

internal sealed record StoredRent(StoredValue<decimal> CadastralIncome, StoredValue<decimal> ShareLet, StoredValue<decimal> Rent);

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
internal sealed partial class PlanJson : JsonSerializerContext;
