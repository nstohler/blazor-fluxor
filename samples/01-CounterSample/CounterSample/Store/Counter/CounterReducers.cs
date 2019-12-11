using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazor.Fluxor;
using CounterSample.Store.Counter.Increment;

namespace CounterSample.Store.Counter
{
    public class CounterReducers
    {
        [ReducerMethod]
        public CounterState Reduce(CounterState state, IncrementCounterAction action)
        {
            return new CounterState(state.ClickCount + 1, state.IsEven);
        }

        [ReducerMethod]
        public CounterState Reduce(CounterState state, IncrementCounterResultAction action)
        {
            return state;
        }

        [ReducerMethod]
        public CounterState Reduce(CounterState state, IncrementCounterIsNowEvenResultAction action)
        {
            return new CounterState(state.ClickCount, action.IsEven);
        }
    }
}
