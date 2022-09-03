using SDParamsDescripter.Core.Models;

namespace Test;

public class PromptSplitterTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ’Êí‚Ì•ªŠ„()
    {
        var splitter = new PromptSplitter(25);

        var result = splitter.Split("aaa, bbb, ccc, ddd, eee").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo("prompt(1/3):\r\naaa, bbb,"));
            Assert.That(result[1], Is.EqualTo("prompt(2/3):\r\nccc, ddd,"));
            Assert.That(result[2], Is.EqualTo("prompt(3/3):\r\neee"));
        });
    }

    [Test]
    public void ’·•¶‚Ì•ªŠ„()
    {
        var splitter = new PromptSplitter(30);

        var result = splitter.Split("aaa bbb ccc ddd eee, fff").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo("prompt(1/2):\r\naaa bbb ccc"));
            Assert.That(result[1], Is.EqualTo("prompt(2/2):\r\nddd eee, fff"));
        });
    }

    [Test]
    public void ’·‚¢’PŒê‚Ì•ªŠ„()
    {
        var splitter = new PromptSplitter(25);

        var result = splitter.Split("aaaaaaaaaaaa bbb ccc, ddd eee fff").ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo("prompt(1/5):\r\naaaaaaaaa"));
            Assert.That(result[1], Is.EqualTo("prompt(2/5):\r\naaa bbb"));
            Assert.That(result[2], Is.EqualTo("prompt(3/5):\r\nccc,"));
            Assert.That(result[3], Is.EqualTo("prompt(4/5):\r\nddd eee"));
            Assert.That(result[4], Is.EqualTo("prompt(5/5):\r\nfff"));
        });
    }
}