using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Fluxor
{
	public class ReactionItem
	{
		public Action<object> Action     { get; set; }
		public Type           ActionType { get; set; }

		public Guid     Guid      { get; set; }
		public DateTime TimeStamp { get; set; }
	}
}