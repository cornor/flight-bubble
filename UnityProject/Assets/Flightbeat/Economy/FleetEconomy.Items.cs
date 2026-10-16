namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54103 / f54299. Signed count check after unsigned kind guard.
        public bool Consume(int kind)
        {
            if (unchecked((uint)kind) > 2u || Data.items[kind] <= 0) return false;
            Commit(d => { d.items[kind] = unchecked(d.items[kind] - 1); d.dayItems = unchecked(d.dayItems + 1); });
            return true;
        }
        // f53975 / f54300. Source does not add an amount > 0 guard; retain its
        // wrapping addition and the existing Commit/Normalize persistence contract.
        public bool GrantItem(int kind, int amount)
        {
            if (unchecked((uint)kind) > 2u || unchecked(Data.items[kind] + amount) > 99) return false;
            Commit(d => d.items[kind] = unchecked(amount + d.items[kind]));
            return true;
        }
    }
}
