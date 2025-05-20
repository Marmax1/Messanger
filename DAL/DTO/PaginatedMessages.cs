using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DTO
{
	public class PaginatedMessages
	{
		public IEnumerable<object> Messages { get; set; }
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
	}
}
