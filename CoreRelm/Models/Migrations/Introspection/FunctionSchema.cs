using CoreRelm.Attributes;
using CoreRelm.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.ForeignKeys;
using static CoreRelm.Enums.SecurityEnums;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class FunctionSchema : RelmModel
    {
        public string? Delimiter { get; set; } = "$$"; // delimiter used in the function definition (e.g. $$) is local so no RelmColumn attribute needed

        [RelmColumn]
        public string? RoutineSchema { get; set; }

        [RelmColumn]
        public string? RoutineName { get; set; }

        [RelmColumn]
        public string? SpecificName { get; set; }

        private string? _routineType;
        [RelmColumn]
        public string? RoutineType
        {
            get => _routineType;
            set
            {
                _routineType = value;
                _routineTypeValue = value?.ParseEnumerationDescription<ProcedureType>();
            }
        }

        private ProcedureType? _routineTypeValue;
        [RelmColumn]
        public ProcedureType? RoutineTypeValue
        {
            get => _routineTypeValue;
            set
            {
                _routineTypeValue = value;
                _routineType = value?.ToDescriptionString();
            }
        }

        [RelmColumn]
        public string? RoutineComment { get; set; }

        [RelmColumn]
        public string? DataType { get; set; }

        [RelmColumn]
        public long? CharacterMaximumLength { get; set; }

        [RelmColumn]
        public long? NumericPrecision { get; set; }

        [RelmColumn]
        public long? NumericScale { get; set; }

        [RelmColumn]
        public long? DatetimePrecision { get; set; }

        [RelmColumn]
        public string? DtdIdentifier { get; set; }

        [RelmColumn]
        public string? RoutineDefinition { get; set; }

        private string? _sqlDataAccess;
        [RelmColumn]
        public string? SqlDataAccess // NO SQL, CONTAINS SQL, READS SQL DATA, MODIFIES SQL DATA
        { 
            get => _sqlDataAccess;
            set
            {
                _sqlDataAccess = value;
                _sqlDataAccessValue = value?.ParseEnumerationDescription<ProcedureDataAccess>();
            }
        }

        private ProcedureDataAccess? _sqlDataAccessValue;
        public ProcedureDataAccess? SqlDataAccessValue // NoSql, ContainsSql, ReadsSqlData, ModifiesSqlData
        {
            get => _sqlDataAccessValue;
            set
            {
                _sqlDataAccessValue = value;
                _sqlDataAccess = value?.ToDescriptionString();
            }
        }

        [RelmColumn]
        public SqlSecurityLevel SecurityType { get; set; }

        private string? _isDeterministic;
        [RelmColumn]
        public string? IsDeterministic 
        {
            get => _isDeterministic;
            set
            {
                _isDeterministic = value;
                _isDeterministicValue = value == "YES";
            }
        }

        private bool? _isDeterministicValue;
        public bool IsDeterministicValue
        {
            get => _isDeterministicValue ?? (_isDeterministic == "YES");
            set
            {
                _isDeterministicValue = value;
                _isDeterministic = value ? "YES" : "NO";
            }
        }

        [RelmColumn]
        public string? SqlMode { get; set; }

        [RelmForeignKey(foreignKey: nameof(FunctionParameterSchema.SpecificName), localKey: nameof(SpecificName), onDelete: ReferentialAction.Cascade)]
        public virtual ICollection<FunctionParameterSchema>? FunctionParameters { get; set; }
    }
}
