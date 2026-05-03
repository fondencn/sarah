using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sarah.API.BusinessObjects.DTOs;
using Sarah.Rules.Services;
using Sarah.Rules.Services.Kernel;
using Sarah.Rules.WebApi.Controllers;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;

namespace Sarah.Rules.Tests;

public class RulesControllerKernelConversationHistoryTests
{
    [Fact]
    public async Task GetKernelConversationHistory_DefaultsToTwoHoursAndSortsNewestFirst()
    {
        await using var db = CreateDbContext();
        SeedConversationMessages(db);

        var controller = CreateController(db);

        ActionResult<IEnumerable<KernelConversationMessageDto>> actionResult = await controller.GetKernelConversationHistory();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var entries = Assert.IsAssignableFrom<IEnumerable<KernelConversationMessageDto>>(ok.Value).ToList();

        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].CreatedAtUtc >= entries[1].CreatedAtUtc);
        Assert.All(entries, entry => Assert.True(entry.CreatedAtUtc >= DateTime.UtcNow.AddHours(-2.1)));
    }

    [Fact]
    public async Task GetKernelConversationHistory_InvalidHours_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        ActionResult<IEnumerable<KernelConversationMessageDto>> actionResult = await controller.GetKernelConversationHistory(hours: 0);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task TruncateKernelConversationHistory_RemovesAllRows()
    {
        await using var db = CreateDbContext();
        SeedConversationMessages(db);
        db.KernelConversationMessages.Add(new KernelConversationMessageEntity
        {
            ConversationId = "another-conversation",
            Role = "user",
            Content = "third-conversation-message",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        ActionResult<object> actionResult = await controller.TruncateKernelConversationHistory();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var payload = Assert.IsAssignableFrom<object>(ok.Value);

        Assert.NotNull(payload);
        Assert.Equal(0, await db.KernelConversationMessages.CountAsync());
    }

    [Fact]
    public async Task SendKernelChatMessage_EmptyMessage_ReturnsBadRequest()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        ActionResult<KernelChatMessageResponseDto> actionResult = await controller.SendKernelChatMessage(
            new KernelChatMessageRequestDto { Message = "   " });

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void SeedConversationMessages(ApplicationDbContext db)
    {
        DateTime now = DateTime.UtcNow;

        db.KernelConversationMessages.AddRange(
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "user",
                Content = "latest",
                CreatedAtUtc = now.AddMinutes(-10)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "assistant",
                Content = "second-latest",
                CreatedAtUtc = now.AddMinutes(-80)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "user",
                Content = "old-entry",
                CreatedAtUtc = now.AddHours(-3)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "other-conversation",
                Role = "assistant",
                Content = "should-not-appear",
                CreatedAtUtc = now.AddMinutes(-5)
            });

        db.SaveChanges();
    }

    private static RulesController CreateController(ApplicationDbContext db)
    {
        return new RulesController(
            ruleService: null!,
            alarmService: null!,
            temperatureService: null!,
            db: db,
            promptProvider: null!,
            promptRuleStore: null!,
            kernelService: null!,
            logger: NullLogger<RulesController>.Instance);
    }
}
