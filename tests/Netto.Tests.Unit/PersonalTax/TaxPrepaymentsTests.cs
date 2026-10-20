using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

public class TaxPrepaymentsTests
{
    [Fact]
    public void None_withholdsAndPrepaysNothing()
    {
        (TaxPrepayments.None.Withheld, TaxPrepayments.None.Paid.ByDeadline.Sum(), TaxPrepayments.None.Starter).ShouldBe(((decimal?)0m, 0m, false));
    }

    [Fact]
    public void AsWithheld_leavesTheWithholdingToTheKeyFormulaAndPrepaysNothing()
    {
        (TaxPrepayments.AsWithheld.Withheld, TaxPrepayments.AsWithheld.Paid.ByDeadline.Sum(), TaxPrepayments.AsWithheld.Starter)
            .ShouldBe(((decimal?)null, 0m, false));
    }
}
