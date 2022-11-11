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
            ccc: 222
            """;

        var desc = PngChunkDescriptionSeparator.Parse(text, "bbb");
        
        Console.WriteLine(desc.FullParameters);

        Assert.That(desc.FullParameters, Does.Not.Contain("bbb"));
        Assert.That(desc.FullParameters, Does.Not.Contain("111"));
    }
}
