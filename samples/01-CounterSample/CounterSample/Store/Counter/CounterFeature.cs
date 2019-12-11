using Blazor.Fluxor;

namespace CounterSample.Store.Counter
{
	public class CounterFeature : Feature<CounterState>
	{
		public override string GetName() => "Counter";
		protected override CounterState GetInitialState() => new CounterState(0, true);
	}
}
