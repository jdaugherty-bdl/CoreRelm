using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.SecurityEnums;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.Attributes.BaseClasses
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class RelmStoredProcedure(
        string name,
        string body,
        string returnType = null,
        int returnSize = -1,
        string comment = null,
        string language = null,
        bool isDeterministic = false,
        ProcedureDataAccess dataAccess = ProcedureDataAccess.None,
        SqlSecurityLevel securityLevel = SqlSecurityLevel.None) : Attribute
    {
        public object? ProcedureKeyHolder { get; set; } 

        public List<RelmProcedureParameterBase>? ProcedureParameters { get; set; }

        public string Name { get; set; } = name;

        public string Body { get; set; } = body;

        public string? ReturnType { get; set; } = returnType;

        public int? ReturnSize { get; set; } = returnSize;

        public string? Comment { get; set; } = comment;

        public string? Language { get; set; } = language;

        public bool IsDeterministic { get; set; } = isDeterministic;

        public ProcedureDataAccess DataAccess { get; set; } = dataAccess;

        public SqlSecurityLevel SecurityLevel { get; set; } = securityLevel;
    }
}
