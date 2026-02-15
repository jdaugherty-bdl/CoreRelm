using CoreRelm.Attributes;
using CoreRelm.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.ForeignKeys;
using static CoreRelm.Enums.StoredProcedures;

namespace CoreRelm.Models.Migrations.Introspection
{
    public class FunctionParameterSchema : RelmModel
    {
        [RelmColumn]
        public string? SpecificName { get; set; }

        [RelmColumn]
        public long? OrdinalPosition { get; set; }

        private string? _parameterMode;
        [RelmColumn]
        public string? ParameterMode // IN, OUT, INOUT
        {
            get => _parameterMode;
            set
            {
                _parameterMode = value;
                _parameterModeValue = value?.ParseEnumerationDescription<ParameterDirection>();
            }
        }

        private ParameterDirection? _parameterModeValue;
        public ParameterDirection? ParameterModeValue // In, Out, InOut
        { 
            get => _parameterModeValue;
            set
            {
                _parameterModeValue = value;
                _parameterMode = value?.ToDescriptionString();
            }
        }

        [RelmColumn]
        public string? ParameterName { get; set; }

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

        [RelmForeignKey(foreignKey: nameof(FunctionSchema.SpecificName), localKey: nameof(SpecificName), onDelete: ReferentialAction.Cascade)]
        public FunctionSchema? Function { get; set; }
    }
}
