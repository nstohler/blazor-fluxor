using Blazor.Fluxor.Reactions;

namespace CounterSample.Store.Counter.Increment
{
	public class IncrementCounterAction : ActionWithReactionBase
    {
        // - store action GUID here, 
        // - provide option to create Reaction action only through this action?

        public IncrementCounterResultAction CreateResultReaction()
        {
            var reaction = new IncrementCounterResultAction()
            {
                ParentActionGuid = this.ActionGuid
            };
            return reaction;
        }
	}
}
