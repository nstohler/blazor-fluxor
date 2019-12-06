using Blazor.Fluxor;

namespace CounterSample.Store.Counter
{
	public class IncrementCounterAction
	{
	}

    public class IncrementCounterResultAction 
        //: IResultAction<IncrementCounterResultAction>
    {
        public string Message { get; set; }
        public int Count { get; set; }
    }

    public class IncrementCounterIsNowEvenResultAction
    {
        public bool IsEven { get; set; }
    }
}
