using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCServer
{
    class InforProc : IEnumerable
    {
        private string[] array = null;

        public InforProc(string s, char split = ' ')
        {
            //array = s.Split(split);
            //自定义分割，支持""内的不分割

            try
            {
                s += split;
                List<string> t = new List<string>();
                int len = s.Length, subpos = 0;
                bool special = true, vaild = false;
                for (int i = 0; i < len; ++i)
                {
                    if (s[i] == '\"')
                        special = !special;
                    else if (s[i] == split)
                    {
                        if (vaild && special)
                        {
                            t.Add(s.Substring(subpos, i - subpos).Replace("\"", ""));
                        }
                        if (special) subpos = i + 1;
                        vaild = false;
                    }
                    else vaild = true;
                }

                array = t.ToArray();
            }
            catch (Exception) { }
        }

        public string this[int index]
        {
            get
            {
                if (index < 0 || index >= array.Length) return "Null";
                return array[index];
            }
        }

        public int Count
        {
            get
            {
                return array.Length;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (string s in array)
                yield return s;
        }
    }
}
