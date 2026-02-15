using CoreRelm.Attributes.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class RelmProcedureParameter<T> : RelmProcedureParameterBase
    {
        public RelmProcedureParameter(
            T parameterKey,
            ParameterDirection parameterDirection,
            string name,
            string dbType,
            int size) : base(parameterDirection, name, dbType, size)
        {
            if (parameterKey == null)
                throw new ArgumentException("Parameter key cannot be null.", nameof(parameterKey));

            ParameterKey = parameterKey;
            ProcedureKeyHolder = parameterKey;
        }

        public T ParameterKey { get; set; }
    }
}
