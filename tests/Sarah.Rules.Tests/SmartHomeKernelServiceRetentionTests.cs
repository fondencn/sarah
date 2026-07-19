using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sarah.Rules.Services.Kernel;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;

namespace Sarah.Rules.Tests;

public class SmartHomeKernelServiceRetentionTests
{
    [Fact]
    public async Task QueryRetainedConversationMessages_ExcludesToolRoleMessages()
    {
        await using var db = CreateDbContext();
        DateTime now = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc);

        db.KernelConversationMessages.AddRange(
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "user",
                Content = "retained-user",
                CreatedAtUtc = now.AddMinutes(-5)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "tool",
                Content = "excluded-tool",
                CreatedAtUtc = now.AddMinutes(-4)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "assistant",
                Content = "retained-assistant",
                CreatedAtUtc = now.AddMinutes(-3)
            });

        await db.SaveChangesAsync();

        var service = CreateService(retentionHours: 24, maxHistoryMessages: 200);

        var retained = await service.QueryRetainedConversationMessages(db, now)
            .ToListAsync();

        Assert.Equal(2, retained.Count);
        Assert.DoesNotContain(retained, m => m.Role == "tool");
        Assert.Equal("retained-user", retained[0].Content);
        Assert.Equal("retained-assistant", retained[1].Content);
    }

    [Fact]
    public async Task QueryRetainedConversationMessages_FiltersByCutoffAndConversation()
    {
        await using var db = CreateDbContext();
        DateTime now = new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc);
        DateTime cutoff = now.AddHours(-24);

        db.KernelConversationMessages.AddRange(
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "user",
                Content = "included-boundary",
                CreatedAtUtc = cutoff
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "assistant",
                Content = "included-new",
                CreatedAtUtc = now.AddHours(-1)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "user",
                Content = "excluded-old",
                CreatedAtUtc = cutoff.AddSeconds(-1)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "other-conversation",
                Role = "assistant",
                Content = "excluded-other-conversation",
                CreatedAtUtc = now.AddMinutes(-5)
            });

        await db.SaveChangesAsync();

        var service = CreateService(retentionHours: 24, maxHistoryMessages: 200);

        var retained = await service.QueryRetainedConversationMessages(db, now)
            .ToListAsync();

        Assert.Equal(2, retained.Count);
        Assert.All(retained, m => Assert.Equal("smart-home-main", m.ConversationId));
        Assert.All(retained, m => Assert.True(m.CreatedAtUtc >= cutoff));
        Assert.Equal("included-boundary", retained[0].Content);
        Assert.Equal("included-new", retained[1].Content);
    }

    [Fact]
    public async Task PruneConversationHistoryAsync_RemovesExpiredRows()
    {
        await using var db = CreateDbContext();
        DateTime now = DateTime.UtcNow;

        db.KernelConversationMessages.AddRange(
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "user",
                Content = "expired",
                CreatedAtUtc = now.AddHours(-25)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "smart-home-main",
                Role = "assistant",
                Content = "retained",
                CreatedAtUtc = now.AddHours(-2)
            },
            new KernelConversationMessageEntity
            {
                ConversationId = "other-conversation",
                Role = "assistant",
                Content = "other-conversation-old",
                CreatedAtUtc = now.AddHours(-100)
            });

        await db.SaveChangesAsync();

        var service = CreateService(retentionHours: 24, maxHistoryMessages: 200);

        int removed = await service.PruneConversationHistoryAsync(db, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(1, removed);

        var allRows = await db.KernelConversationMessages
            .OrderBy(m => m.Content)
            .Select(m => m.Content)
            .ToListAsync();

        Assert.Equal(2, allRows.Count);
        Assert.Contains("retained", allRows);
        Assert.Contains("other-conversation-old", allRows);
        Assert.DoesNotContain("expired", allRows);
    }

    private static SmartHomeKernelService CreateService(int retentionHours, int maxHistoryMessages)
    {
        var options = Options.Create(new SemanticKernelOptions
        {
            ConversationRetentionHours = retentionHours,
            MaxHistoryMessages = maxHistoryMessages
        });

        return new SmartHomeKernelService(
            scopeFactory: null!,
            options: options,
            promptProvider: null!,
            logger: NullLogger<SmartHomeKernelService>.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
