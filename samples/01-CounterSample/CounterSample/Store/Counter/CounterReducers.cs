using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazor.Fluxor;

namespace CounterSample.Store.Counter
{
    public class CounterReducers
    {
        [ReducerMethod]
        public CounterState Reduce(CounterState state, IncrementCounterAction action)
        {
            return new CounterState(state.ClickCount + 1);
        }

        [ReducerMethod]
        public CounterState Reduce(CounterState state, IncrementCounterResultAction action)
        {
            return state;
        }
    }
}
