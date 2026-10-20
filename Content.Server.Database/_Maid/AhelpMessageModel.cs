using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

/// <summary>
/// One message of an ahelp conversation, kept across rounds.
/// </summary>
[Table("maid_ahelp_messages")]
[Index(nameof(PlayerUserId), nameof(Id))]
public class AhelpMessage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey("Round")]
    public int? RoundId { get; set; }
    public Round? Round { get; set; }

    /// <summary>
    /// The player whose ahelp thread this message belongs to.
    /// </summary>
    public Guid PlayerUserId { get; set; }

    /// <summary>
    /// Who sent the message. <see cref="Guid.Empty"/> for messages relayed from Discord.
    /// </summary>
    public Guid SenderUserId { get; set; }

    /// <summary>
    /// Sender name at the time of sending (ckey or Discord username).
    /// </summary>
    [MaxLength(256)]
    public string SenderName { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    /// <summary>
    /// Raw message text, without markup added by the bwoink system.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public bool SenderIsAdmin { get; set; }

    public bool AdminOnly { get; set; }

    public bool PlaySound { get; set; }

    public bool FromWebhook { get; set; }
}
