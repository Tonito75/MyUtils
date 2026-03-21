using MonsterHub.Api.Models;
using MonsterHub.Api.Services;

namespace MonsterHub.Tests.Services;

public class MonsterMatchingServiceTests
{
    private static List<MonsterMapping> BuildMappings() =>
    [
        new() { Id = 1, Name = "Ultra White", Emoji = "⚪",
            KeywordsJson = """["ultra white","white monster"]""" },
        new() { Id = 2, Name = "The Original", Emoji = "🟢",
            KeywordsJson = """["the original","original energy"]""" },
        new() { Id = 3, Name = "Ultra Gold", Emoji = "🟡",
            KeywordsJson = """["ultra gold","gold"]""" },
        new() { Id = 4, Name = "Fake Gold", Emoji = "🟤",
            KeywordsJson = """["gold","fake gold"]""" },
    ];

    [Fact]
    public void Match_ExactKeyword_ReturnsCorrectMapping()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("ultra white", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Match_CaseInsensitive_ReturnsMatch()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("ULTRA WHITE", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Match_PartialContains_ReturnsMatch()
    {
        var svc = new MonsterMatchingService();
        // Mistral might return "Monster Ultra Gold can"
        var result = svc.Match("Monster Ultra Gold can", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public void Match_NoMatch_ReturnsNull()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("some random text", BuildMappings());
        Assert.Null(result);
    }

    [Fact]
    public void Match_EmptyString_ReturnsNull()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match("", BuildMappings());
        Assert.Null(result);
    }

    [Fact]
    public void Match_NullInput_ReturnsNull()
    {
        var svc = new MonsterMatchingService();
        var result = svc.Match(null!, BuildMappings());
        Assert.Null(result);
    }

    [Fact]
    public void Match_FirstMatchWins_WhenAmbiguous()
    {
        var svc = new MonsterMatchingService();
        // "gold" matches both Id=3 ("ultra gold","gold") and Id=4 ("gold","fake gold")
        // proves first-by-Id ordering wins
        var result = svc.Match("gold", BuildMappings());
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }
}
