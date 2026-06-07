using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Sarah.Rules.WebApi.Data;
using Sarah.Rules.WebApi.Data.Entities;

namespace Sarah.Rules.Services.Kernel;

public sealed class RulesMemoryKernelPlugin
{
    private const string ConversationId = "smart-home-main";

    private readonly ApplicationDbContext _db;

    public RulesMemoryKernelPlugin(ApplicationDbContext db)
    {
        _db = db;
    }

    [KernelFunction, Description("Liefert die letzten Konversationseintraege aus dem Rule-Memory.")]
    public async Task<string> GetRecentConversationAsync(int limit = 10)
    {
        int safeLimit = Math.Clamp(limit, 1, 50);
        var rows = await _db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(safeLimit)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return "Keine Konversationseintraege vorhanden.";
        }

        rows.Reverse();
        return string.Join(Environment.NewLine,
            rows.Select(r => $"[{r.CreatedAtUtc:O}] {r.Role}: {r.Content}"));
    }

    [KernelFunction, Description("Sucht in den letzten Konversationseintraegen nach einem Stichwort.")]
    public async Task<string> SearchConversationAsync(string keyword, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return "Bitte ein Suchstichwort angeben.";
        }

        int safeLimit = Math.Clamp(limit, 1, 50);
        string needle = keyword.Trim().ToLowerInvariant();

        var rows = await _db.KernelConversationMessages
            .Where(m => m.ConversationId == ConversationId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(200)
            .ToListAsync();

        var matches = rows
            .Where(r => r.Content.ToLower().Contains(needle))
            .Take(safeLimit)
            .ToList();

        if (matches.Count == 0)
        {
            return $"Keine Treffer fuer '{keyword}' gefunden.";
        }

        matches.Reverse();
        return string.Join(Environment.NewLine,
            matches.Select(r => $"[{r.CreatedAtUtc:O}] {r.Role}: {r.Content}"));
    }

    [KernelFunction, Description("Speichert eine kurze Notiz im Rule-Memory fuer spaetere Entscheidungen.")]
    public async Task<string> RememberAsync(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return "Leere Notiz wurde nicht gespeichert.";
        }

        _db.KernelConversationMessages.Add(new KernelConversationMessageEntity
        {
            ConversationId = ConversationId,
            Role = "tool",
            Content = "memory-note: " + note.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return "Notiz gespeichert.";
    }

    [KernelFunction, Description("Prueft, ob ein Text in den letzten Minuten bereits in der Konversation vorkam.")]
    public async Task<string> WasMentionedRecentlyAsync(string text, int withinMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Bitte einen Text zum Pruefen angeben.";
        }

        int safeMinutes = Math.Clamp(withinMinutes, 1, 1440);
        var cutoff = DateTime.UtcNow.AddMinutes(-safeMinutes);
        string needle = text.Trim().ToLowerInvariant();

        bool exists = await _db.KernelConversationMessages
            .AnyAsync(m => m.ConversationId == ConversationId
                           && m.CreatedAtUtc >= cutoff
                           && m.Content.ToLower().Contains(needle));

        return exists
            ? $"Ja, der Text wurde in den letzten {safeMinutes} Minuten bereits erwaehnt."
            : $"Nein, der Text wurde in den letzten {safeMinutes} Minuten nicht erwaehnt.";
    }
}