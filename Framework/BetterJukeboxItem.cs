using System;

namespace Gaphodil.BetterJukebox.Framework
{
    public class BetterJukeboxItem : IComparable<BetterJukeboxItem>
    {
        public string Name;
        public bool IsLocked;
        public int LockCount;
        public int ShakeTimer = 0;

        public BetterJukeboxItem(string name, bool isLocked = false, int lockCount = 0)
        {
            Name = name;
            IsLocked = isLocked;
            LockCount = lockCount;
        }

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
