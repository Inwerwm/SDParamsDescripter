using SDParamsDescripter.Core.Models;

namespace Test;
internal class StableDiffusionWebUIDescriptionSeparatorTest
{
    [Test]
    public void 動作テスト()
    {
        var sp = new StableDiffusionWebUIDescriptionSeparator();
        var rep = sp.Separate("F:\\Generated\\黒フリル\\00007-250_k_euler_a_2841080817.yaml");

        Console.WriteLine(rep.Numbers);
        foreach (var item in rep.PromptReplies)
        {
            Console.WriteLine(item);
        }
    }

    [Test]
    public void 名前変換テスト()
    {
        var conv = new SnakeCaseNamingConvention();

        var testValue = "TestCase";
        var result = conv.Convert(testValue);

        Assert.That(result, Is.EqualTo("test_case"));
    }
}
