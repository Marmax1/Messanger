using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Message
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public int UserId { get; set; }

    public int ChatRoomId { get; set; }

    public virtual ChatRoom ChatRoom { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
