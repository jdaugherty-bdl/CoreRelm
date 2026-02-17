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
    [RelmDatabase("INFORMATION_SCHEMA")]
    [RelmTable("ROUTINES")]
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

        private string? _dataType;
        [RelmColumn]
        public string? DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                DtdIdentifierValue = GetDtdIdentifier(_dataType, CharacterMaximumLength, NumericPrecision, NumericScale, DatetimePrecision);
            }
        }

        private long? _characterMaximumLength;
        [RelmColumn]
        public long? CharacterMaximumLength 
        { 
            get => _characterMaximumLength;
            set
            {
                _characterMaximumLength = value;
                DtdIdentifierValue = GetDtdIdentifier(DataType, _characterMaximumLength, NumericPrecision, NumericScale, DatetimePrecision);
            }
        }

        private long? _numericPrecision;
        [RelmColumn]
        public long? NumericPrecision
        {
            get => _numericPrecision;
            set
            {
                _numericPrecision = value;
                DtdIdentifierValue = GetDtdIdentifier(DataType, CharacterMaximumLength, _numericPrecision, NumericScale, DatetimePrecision);
            }
        }

        private long? _numericScale;
        [RelmColumn]
        public long? NumericScale
        {
            get => _numericScale;
            set
            {
                _numericScale = value;
                DtdIdentifierValue = GetDtdIdentifier(DataType, CharacterMaximumLength, NumericPrecision, _numericScale, DatetimePrecision);
            }
        }

        private long? _datetimePrecision;
        [RelmColumn]
        public long? DatetimePrecision
        {
            get => _datetimePrecision;
            set
            {
                _datetimePrecision = value;
                DtdIdentifierValue = GetDtdIdentifier(DataType, CharacterMaximumLength, NumericPrecision, NumericScale, _datetimePrecision);
            }
        }

        private string? _dtdIdentifier;
        [RelmColumn]
        public string? DtdIdentifier 
        { 
            get => _dtdIdentifier;
            set
            {
                _dtdIdentifier = value;
                _dtdIdentifierValue = value;

                if (value != null) 
                {
                    DataType = GetDataTypeFromDtdIdentifier(value);
                    CharacterMaximumLength = GetCharacterMaximumLengthFromDtdIdentifier(value);
                    NumericPrecision = GetNumericPrecisionFromDtdIdentifier(value);
                    NumericScale = GetNumericScaleFromDtdIdentifier(value);
                    DatetimePrecision = GetDatetimePrecisionFromDtdIdentifier(value);
                }
            }
        }

        private string? _dtdIdentifierValue;
        public string? DtdIdentifierValue 
        { 
            get => _dtdIdentifierValue;
            set
            {
                _dtdIdentifierValue = value;
                _dtdIdentifier = GetDtdIdentifier(DataType, CharacterMaximumLength, NumericPrecision, NumericScale, DatetimePrecision);
            }
        }

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

        private static string? GetDtdIdentifier(
            string? dataType,
            long? characterMaximumLength,
            long? numericPrecision,
            long? numericScale,
            long? datetimePrecision)
        {
            if (dataType == null)
                return null;

            return dataType.ToLower() switch
            {
                // String types: varchar(255), char(10)
                "varchar" or "char" or "varbinary" or "binary"
                    when characterMaximumLength.HasValue => $"{dataType}({characterMaximumLength})",

                // Exact numeric types: decimal(10,2)
                "decimal" or "numeric"
                    when numericPrecision.HasValue && numericScale.HasValue => $"{dataType}({numericPrecision},{numericScale})",

                // Integer and Bit types: int(11), tinyint(3), bit(8)
                "int" or "tinyint" or "smallint" or "mediumint" or "bigint" or "bit"
                    when numericPrecision.HasValue => $"{dataType}({numericPrecision})",

                // Floating point types: float(7,4) or double
                "float" or "double"
                    when numericPrecision.HasValue && numericScale.HasValue => $"{dataType}({numericPrecision},{numericScale})",
                "float" or "double"
                    when numericPrecision.HasValue => $"{dataType}({numericPrecision})",

                // Temporal types with fractional seconds: datetime(6), time(3)
                "datetime" or "time" or "timestamp"
                    when datetimePrecision.HasValue => $"{dataType}({datetimePrecision})",

                // Types with no parameters: text, blob, date, year
                _ => dataType
            };
        }

        private static string? GetDataTypeFromDtdIdentifier(string? dtdIdentifier)
        {
            if (dtdIdentifier == null)
                return null;
            
            var index = dtdIdentifier.IndexOf('(');
            
            return index > 0 ? dtdIdentifier[..index] : dtdIdentifier;
        }

        private static long? GetCharacterMaximumLengthFromDtdIdentifier(string? dtdIdentifier)
        {
            if (dtdIdentifier == null)
                return null;
            
            var dataType = GetDataTypeFromDtdIdentifier(dtdIdentifier)?.ToLower();
            
            if (dataType == "varchar" || dataType == "char" || dataType == "varbinary" || dataType == "binary")
            {
                var start = dtdIdentifier.IndexOf('(');
                var end = dtdIdentifier.IndexOf(')');
                if (start > 0 && end > start)
                {
                    if (long.TryParse(dtdIdentifier.AsSpan(start + 1, end - start - 1), out var result))
                        return result;
                }
            }

            return null;
        }

        private static long? GetNumericPrecisionFromDtdIdentifier(string? dtdIdentifier)
        {
            if (dtdIdentifier == null)
                return null;
            
            var dataType = GetDataTypeFromDtdIdentifier(dtdIdentifier)?.ToLower();
            
            if (dataType == "decimal" || dataType == "numeric" || dataType == "int" || dataType == "tinyint" || dataType == "smallint" || dataType == "mediumint" || dataType == "bigint" || dataType == "bit" || dataType == "float" || dataType == "double")
            {
                var start = dtdIdentifier.IndexOf('(');
                var end = dtdIdentifier.IndexOf(')');
                
                if (start > 0 && end > start)
                {
                    var parameters = dtdIdentifier.Substring(start + 1, end - start - 1).Split(',');
                    if (long.TryParse(parameters[0].Trim(), out var result))
                        return result;
                }
            }

            return null;
        }

        private static long? GetNumericScaleFromDtdIdentifier(string? dtdIdentifier)
        {
            if (dtdIdentifier == null)
                return null;

            var dataType = GetDataTypeFromDtdIdentifier(dtdIdentifier)?.ToLower();
            
            if (dataType == "decimal" || dataType == "numeric" || dataType == "float" || dataType == "double")
            {
                var start = dtdIdentifier.IndexOf('(');
                var end = dtdIdentifier.IndexOf(')');
            
                if (start > 0 && end > start)
                {
                    var parameters = dtdIdentifier.Substring(start + 1, end - start - 1).Split(',');
                    if (parameters.Length > 1)
                    {
                        if (long.TryParse(parameters[1].Trim(), out var result))
                            return result;
                    } 
                    else 
                    {
                        // For float and double, if only one parameter is provided, it represents precision and scale is 0
                        if ((dataType == "float" || dataType == "double") && long.TryParse(parameters[0].Trim(), out var precision))
                            return 0;
                    }
                }
            }
            
            return null;
        }

        private static long? GetDatetimePrecisionFromDtdIdentifier(string? dtdIdentifier)
        {
            if (dtdIdentifier == null)
                return null;

            var dataType = GetDataTypeFromDtdIdentifier(dtdIdentifier)?.ToLower();
            
            if (dataType == "datetime" || dataType == "time" || dataType == "timestamp")
            {
                var start = dtdIdentifier.IndexOf('(');
                var end = dtdIdentifier.IndexOf(')');
            
                if (start > 0 && end > start)
                    return long.TryParse(dtdIdentifier.AsSpan(start + 1, end - start - 1), out var result) ? result : null;
            }
            
            return null;
        }

        private static string? GetDtdIdentifierFromDataType(string? dataType, long? characterMaximumLength, long? numericPrecision, long? numericScale, long? datetimePrecision)
        {
            if (dataType == null)
                return null;
            return dataType.ToLower() switch
            {
                // String types: varchar(255), char(10)
                "varchar" or "char" or "varbinary" or "binary"
                    when characterMaximumLength.HasValue => $"{dataType}({characterMaximumLength})",
                // Exact numeric types: decimal(10,2)
                "decimal" or "numeric"
                    when numericPrecision.HasValue && numericScale.HasValue => $"{dataType}({numericPrecision},{numericScale})",
                // Integer and Bit types: int(11), tinyint(3), bit(8)
                "int" or "tinyint" or "smallint" or "mediumint" or "bigint" or "bit"
                    when numericPrecision.HasValue => $"{dataType}({numericPrecision})",
                // Floating point types: float(7,4) or double
                "float" or "double"
                    when numericPrecision.HasValue && numericScale.HasValue => $"{dataType}({numericPrecision},{numericScale})",
                "float" or "double"
                    when numericPrecision.HasValue => $"{dataType}({numericPrecision})",
                // Temporal types with fractional seconds: datetime(6), time(3)
                "datetime" or "time" or "timestamp"
                    when datetimePrecision.HasValue => $"{dataType}({datetimePrecision})",
                // Types with no parameters: text, blob, date, year
                _ => dataType
            };
        }
    }
}
