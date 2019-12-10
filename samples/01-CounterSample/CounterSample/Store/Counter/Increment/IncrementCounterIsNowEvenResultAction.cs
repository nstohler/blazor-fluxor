using Blazor.Fluxor.Reactions;

namespace CounterSample.Store.Counter.Increment
{
    public class IncrementCounterIsNowEvenResultAction //: ReactionBase
    {
        public bool IsEven { get; set; }
    }
}