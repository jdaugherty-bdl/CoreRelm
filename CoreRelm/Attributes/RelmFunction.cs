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
    public class RelmFunction<T> : RelmStoredProcedure
    {
        public RelmFunction(
            T procedureKey,
            string name,
            string body,
            string returnType = null,
            int returnSize = -1,
            string comment = null,
            string language = null,
            bool isDeterministic = false,
            ProcedureDataAccess dataAccess = ProcedureDataAccess.None,
            SqlSecurityLevel securityLevel = SqlSecurityLevel.None) 
            : base(
                name,
                body,
                returnType,
                returnSize,
                comment,
                language,
                isDeterministic,
                dataAccess,
                securityLevel)
        {
            if (procedureKey == null)
                throw new ArgumentException("Procedure key cannot be null.", nameof(procedureKey));

            ProcedureKeyHolder = procedureKey;
        }
    }

    public class RelmFunction : RelmStoredProcedure
    {
        public RelmFunction(
            string name,
            string body,
            string returnType = null,
            int returnSize = -1,
            string comment = null,
            string language = null,
            bool isDeterministic = false,
            ProcedureDataAccess dataAccess = ProcedureDataAccess.None,
            SqlSecurityLevel securityLevel = SqlSecurityLevel.None) 
            : base(
                name,
                body,
                returnType,
                returnSize,
                comment,
                language,
                isDeterministic,
                dataAccess,
                securityLevel)
        {
        }
    }
}
