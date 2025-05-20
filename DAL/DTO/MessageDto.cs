using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DTO
{
	internal class MessageDto
	{
		public int Id { get; set; }
		public string Text { get; set; }
		public string SenderNickname { get; set; }
		public DateTime SentAt { get; set; }

	}
}
