using System;

namespace Gaphodil.BetterJukebox.Framework
{
    public class BetterJukeboxItem(string name, bool isLocked = false, int lockCount = 0) : IComparable<BetterJukeboxItem>
    {
        public string Name = name;
        public bool IsLocked = isLocked;
        public int LockCount = lockCount;
        public int ShakeTimer = 0;

        public int CompareTo(BetterJukeboxItem itemB)
        {
            if (itemB is null)
                return 1;
            return Name.CompareTo(itemB.Name);
        }
        public bool Equals(BetterJukeboxItem item)
        {
            if (item is null)
                return false;
            return Name.Equals(item.Name);
        }
    }
}
