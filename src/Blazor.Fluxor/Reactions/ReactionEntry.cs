using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Fluxor.Reactions
{
	public class ReactionEntry
	{
		//public ReactionEntry Root { get; set; }
		public ReactionEntry Parent { get; set; }

		public object             Action        { get; set; }
		public DateTime           DateTime      { get; set; }
		public List<ReactionItem> ReactionItems { get; set; }
		public bool               Invoked       { get; set; }

		//public ReactionEntry GetRoot()
		//{
		//	if (this.Root != null)
		//	{
		//		return this;
		//	}
		//}
		public ReactionEntry GetRoot()
		{
			if (Parent == null)
			{
				return this;
			}

			return Parent.GetRoot();
		}
	}
}