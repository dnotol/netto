using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Rules;

// An article of CIR 92 as the administration coordinates it on Fisconetplus for one income year: its
// title, its address by the record's guid, the file official-docs/ keeps it under, named by the day the
// record was last modified, and the few words a list cites it by.
public class SourceTests
{
    [Fact]
    public void Fisconet_article_citesTheCoordinatedTextOfItsIncomeYear()
    {
        var source = Source.Fisconet("personal-tax", 157, 2026, new DateOnly(2026, 7, 30), "c6e47940-53f0-4608-b567-ae774014a621", "The surcharge");

        (source.Title, source.Reference, source.Url, source.Document).ShouldBe((
            "CIR 92, art. 157 (revenus 2026), coordinated text on Fisconetplus, last modified 30.07.2026",
            "The surcharge",
            new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c6e47940-53f0-4608-b567-ae774014a621"),
            "personal-tax/spf-finances_cir92-art-157-revenus-2026_2026-07-30.html"));
    }

    [Fact]
    public void Fisconet_article_isCitedInAListByItsArticle()
    {
        Source.Fisconet("corporate-tax", 159, 2026, new DateOnly(2026, 2, 9), "ed182474-e88a-4931-91a3-51286d1dc47a", "The surcharge").Label.ShouldBe("CIR 92, art. 159");
    }
}
