using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Fluxor.Reactions
{
    public abstract class ReactionBase
    {
		public string ParentActionGuid { get; set; }
	}
}
