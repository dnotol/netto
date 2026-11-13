namespace Netto.Core.Rules;

// The input a refusal is about, in the river's order: what the page marks as refused, and through it the
// card that holds it. A new input is a value here and its case where the page places it.
public enum RefusedInput
{
    // No input: the income year, or its region, has no rules.
    IncomeYear,

    Revenue,

    RunningCosts,

    MealVoucherDays,

    EcoVouchers,

    SportCultureVouchers,

    OfficeAllowance,

    InternetAllowance,

    ComputerAllowance,

    PhoneCosts,

    CadastralIncome,

    ShareLet,

    // The rent, also when an office allowance is paid beside it.
    Rent,

    // The PCs the company provides, also beside a computer allowance.
    EquipmentPcs,

    // The tablets it provides, also beside a computer allowance.
    EquipmentTablets,

    EquipmentMobilePhones,

    // The internet connection it provides, also beside an internet allowance.
    EquipmentInternet,

    EquipmentPhoneSubscriptions,

    // The day the company provides the set from, and the day the director gives it back on.
    EquipmentProvidedFrom,

    EquipmentReturnedOn,

    EquipmentYearlyCosts,

    // What the director pays the company for the set's private use, also above its flat values.
    EquipmentContribution,

    // What the same things would cost privately, which no rule reads: the card weighs the set against it.
    EquipmentPrivateCost,

    CarCatalogueValue,

    CarCo2,

    // The CO2 of the car a fake plug-in hybrid matches.
    CarMatchingCo2,

    // The date the car was ordered or leased, which also decides whether a plug-in hybrid's fuel is capped.
    CarAcquired,

    // The day the director has the car from, and the day they give it back on.
    CarHeldFrom,

    CarReturnedOn,

    CarYearlyCosts,

    CarContribution,

    // What the same car would cost privately, which no rule reads: the car card weighs the company car
    // against it.
    CarPrivateCost,

    // A fixed copyright royalty, also above what the company has before pay and royalty.
    Royalty,

    CreativeShare,

    // The gross pay on the payslip, and the key formula's month of it.
    DirectorPay,

    // The rate of the social insurance fund's management fee.
    FundFeeRate,

    MunicipalRate,

    TaxWithheld,

    // What the director paid in advance on the income tax, by any deadline.
    DirectorAdvancePayments,

    // What the company paid in advance on its corporate tax, by any deadline.
    CompanyAdvancePayments,

    Reserve,

    EarlierProfits,

    // What the social insurance fund bills each quarter, the payment calendar's own input.
    BilledContributions,

    // The net professional income of the partner the director is taxed jointly with.
    PartnerIncome,

    // The dependent children, the director's own or in a joint return the household's.
    Children,

    // How many of the children are under 3 with no childcare costs claimed for them.
    ChildrenUnder3,

    // The dependants aged 66 or more in a situation of dependence.
    DependantsInCare,

    OtherDependants,
}
