namespace CounterSample.Store.Counter.Increment
{
    public class IncrementCounterResultAction 
        //: IResultAction<IncrementCounterResultAction>
    {
        public string Message { get; set; }
        public int    Count   { get; set; }
    }
}