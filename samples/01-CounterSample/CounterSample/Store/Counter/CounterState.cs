namespace CounterSample.Store.Counter
{
    public class CounterState
    {
        public int  ClickCount { get; private set; }
        public bool IsEven     { get; private set; }

        public CounterState(int clickCount, bool isEven)
        {
            ClickCount = clickCount;
            IsEven     = isEven;
        }
    }
}