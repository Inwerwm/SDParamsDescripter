using SDParamsDescripter.Core.Models;

namespace Test;
internal class PngChunkDescriptionSeparatorTest
{
    [Test]
    public void 除外項目有りの分割()
    {
        var text = """
            abcdefghijklmn
            aaa: 000
            bbb: 111
            BBB: 999
            bbb2: 11
            ccc: 222
            ddd: bbb
            """;

        var desc = PngChunkDescriptionSeparator.Parse(text, "bbb");
        
        Console.WriteLine(desc.FullParameters);

        Assert.That(desc.FullParameters, Does.Not.Contain("bbb:"));
        Assert.That(desc.FullParameters, Does.Not.Contain("111"));
        Assert.That(desc.FullParameters, Does.Not.Contain("BBB:"));
        Assert.That(desc.FullParameters, Does.Not.Contain("999"));
        Assert.That(desc.FullParameters, Does.Contain("bbb2:"));
        Assert.That(desc.FullParameters, Does.Contain("ddd:"));
    }
}
