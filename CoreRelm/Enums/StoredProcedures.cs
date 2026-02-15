using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Enums
{
    public class StoredProcedures
    {
        public enum ProcedureType
        {
            None,
            [Description("PROCEDURE")]
            StoredProcedure,
            [Description("FUNCTION")]
            Function
        }

        public enum ParameterDirection
        {
            None,
            [Description("IN")]
            Input,
            [Description("OUT")]
            Output,
            [Description("INOUT")]
            InputOutput
        }

        public enum ProcedureDataAccess
        {
            None,
            [Description("NO SQL")]
            NoSql,
            [Description("CONTAINS SQL")]
            ContainsSql,
            [Description("READS SQL DATA")]
            ReadsSqlData,
            [Description("MODIFIES SQL DATA")]
            ModifiesSqlData
        }
    }
}
