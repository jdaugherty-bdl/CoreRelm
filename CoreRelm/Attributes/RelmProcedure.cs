using CoreRelm.Attributes.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.SecurityEnums;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.Attributes
{
    public class RelmProcedure : RelmStoredProcedure
    {
        public RelmProcedure(
            string name,
            string body,
            string comment = null,
            string language = null,
            bool isDeterministic = false,
            ProcedureDataAccess dataAccess = ProcedureDataAccess.None,
            SqlSecurityLevel securityLevel = SqlSecurityLevel.None)
            : base(
                name,
                body,
                null,
                -1,
                comment,
                language,
                isDeterministic,
                dataAccess,
                securityLevel)
        {
        }
    }
}
