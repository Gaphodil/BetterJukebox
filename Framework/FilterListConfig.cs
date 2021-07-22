using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaphodil.BetterJukebox.Framework
{
    public class FilterListConfig
    {
        public List<string> content;
        
        public FilterListConfig(string s)
        {
            content = ToList(s);
        }

        private List<string> ToList(string s)
        {
            List<string> l = new List<string> (s.Split(','));
            for (int i = 0; i < l.Count; i++)
            {
                l[i] = l[i].Trim();
            }
            return l;
        }

        public override string ToString()
        {
            return string.Join(",", content.ToArray());
        }
    }
}
