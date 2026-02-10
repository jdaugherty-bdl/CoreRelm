using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    public class Indexes
    {
        public enum IndexType
        {
            None,
            UNIQUE,
            FULLTEXT,
            SPATIAL,
            BTREE,
            HASH
        }

        public enum Visibility
        {
            None,
            VISIBLE,
            INVISIBLE
        }

        public enum Algorithm
        {
            None,
            DEFAULT,
            INPLACE,
            COPY
        }

        public enum LockOption
        {
            None,
            DEFAULT,
            NONE,
            SHARED,
            EXCLUSIVE
        }
    }
}
