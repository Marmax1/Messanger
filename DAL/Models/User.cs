using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class User
{
    public int Id { get; set; }

    public string Nickname { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
