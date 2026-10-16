namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // src: FleetEconomy.CompleteIndependentTutorial f54147;
        // cached noncapturing mutation <CompleteIndependentTutorial>b__159_0 f54249.
        public void CompleteIndependentTutorial()
        {
            if (!Data.independentTutorial) Commit(d => d.independentTutorial = true);
        }

        // f54148 / cached mutation f16182, SaveData byte +100.
        public void CompleteBombTutorial()
        {
            if (!Data.bombTutorial) Commit(d => d.bombTutorial = true);
        }

        // f54149 / f54250, SaveData byte +103; repeated completion is a no-op.
        public void CompleteFinaleTutorial()
        {
            if (!Data.finaleTutorial) Commit(d => d.finaleTutorial = true);
        }
    }
}
