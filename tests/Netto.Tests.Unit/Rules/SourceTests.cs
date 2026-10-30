using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Rules;

// An article of CIR 92, or of its royal decree of execution (AR/CIR 92), as the administration coordinates
// it on Fisconetplus for one income year: the passage of it a rule rests on, its address by the record's
// guid, the file official-docs/ keeps it under, named by the day the record was last modified, and the
// article it is, which the client words its title and its name in a list from (SourceTextTests).
public class SourceTests
{
    [Fact]
    public void Fisconet_article_citesTheCoordinatedTextOfItsIncomeYear()
    {
        var source = Source.Fisconet("personal-tax", "157", 2026, new DateOnly(2026, 7, 30), "c6e47940-53f0-4608-b567-ae774014a621", "surcharged_tax");

        (source.Passage, source.Url, source.Document, source.Article).ShouldBe((
            "surcharged_tax",
            new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c6e47940-53f0-4608-b567-ae774014a621"),
            "personal-tax/spf-finances_cir92-art-157-revenus-2026_2026-07-30.html",
            new FisconetArticle(FisconetCode.Cir92, "157", 2026, new DateOnly(2026, 7, 30))));
    }

    // Art. 38/1 is filed as art-38-1: a file name holds no slash.
    [Fact]
    public void Fisconet_articleWithASlash_isFiledUnderItsNumberDashed()
    {
        var source = Source.Fisconet("personal-tax", "38/1", 2026, new DateOnly(2026, 1, 14), "6a028d41-f93b-43f0-85ce-ec749722675a", "voucher_conditions");

        (source.Document, source.Article?.Number).ShouldBe(("personal-tax/spf-finances_cir92-art-38-1-revenus-2026_2026-01-14.html", "38/1"));
    }

    [Fact]
    public void Fisconet_articleOfTheRoyalDecree_isFiledAsTheArCir92s()
    {
        var source = Source.Fisconet("personal-tax", "18", 2026, new DateOnly(2026, 8, 10), "f3ebfcea-9d61-4d4a-bd76-33f13ef93825", "reference_emission", FisconetCode.ArCir92);

        (source.Document, source.Article?.Code).ShouldBe(("personal-tax/spf-finances_arcir92-art-18-revenus-2026_2026-08-10.html", FisconetCode.ArCir92));
    }
}
