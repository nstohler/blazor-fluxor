using Blazor.Fluxor.Reactions;

namespace CounterSample.Store.Counter.Increment
{
    public class IncrementCounterResultAction // : IHasReaction
    //: ReactionBase
    //: IResultAction<IncrementCounterResultAction>
    {
        public string Message { get; set; }
        public int    Count   { get; set; }
    }
}