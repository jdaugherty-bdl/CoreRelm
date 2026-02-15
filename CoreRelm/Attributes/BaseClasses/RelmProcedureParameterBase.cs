using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.Attributes.BaseClasses
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class RelmProcedureParameterBase(
        ParameterDirection parameterDirection,
        string name,
        string dbType,
        int size
        ) : Attribute
    {
        public  object? ProcedureKeyHolder { get; set; }

        public ParameterDirection Direction { get; set; } = parameterDirection;
        
        public string Name { get; set; } = name;
        
        public string DbType { get; set; } = dbType;
        
        public int Size { get; set; } = size;
    }
}
