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
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public class RelmProcedure(
        string name,
        string body,
        string comment = null,
        string language = null,
        bool isDeterministic = false,
        ProcedureDataAccess dataAccess = ProcedureDataAccess.None,
        SqlSecurityLevel securityLevel = SqlSecurityLevel.None) 
        : RelmStoredProcedure(
            name,
            body,
            null,
            -1,
            comment,
            language,
            isDeterministic,
            dataAccess,
            securityLevel)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    {
    }
}
