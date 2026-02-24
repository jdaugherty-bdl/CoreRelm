using CoreRelm.Attributes;
using CoreRelm.Models;
using CoreRelm.Quickstart.FieldLoaders;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.Enums.Indexes;
using static CoreRelm.Enums.Triggers;

namespace CoreRelm.Quickstart.Models
{
    // Define the database name
    [RelmDatabase("example_database")]

    // Define the table name
    [RelmTable("example_models")]

    // Define a unique constraint on the combination of GroupInternalId and ModelName
    [RelmUnique([nameof(GroupInternalId), nameof(ModelName)])]

    // Define an index on the combination of GroupInternalId, ModelName, and ModelIndex
    [RelmIndexNamed([nameof(GroupInternalId), nameof(ModelName), nameof(ModelIndex)])]

    // Define a grouped index using the Groupings enum to link. This index is on GroupInternalId and ModelIndex, both in descending order.
    [RelmIndex<Groupings>(Groupings.GroupInternalIdModelIndex)]
    [RelmIndexColumn<Groupings>(Groupings.GroupInternalIdModelIndex, columnName: nameof(GroupInternalId), isDescending: true, order: 0)]
    [RelmIndexColumn<Groupings>(Groupings.GroupInternalIdModelIndex, columnName: nameof(ModelIndex), isDescending: true, order: 1)]

    // Define a trigger that runs before an insert operation.
    [RelmTrigger(TriggerTime.BEFORE, TriggerEvent.INSERT, @"
IF NEW.model_index IS NULL OR NEW.model_index = 0 THEN
    SET NEW.model_index = (SELECT COALESCE(MAX(model_index), 0) + 1 FROM example_models WHERE group_InternalId = NEW.group_InternalId);
END IF;")]

    // Define a function that takes an input parameter, modifies an output parameter and an input-output parameter, and returns a string value.
    [RelmFunction<Groupings>(Groupings.ExampleFunction, "example_function", @"
        DECLARE result_val VARCHAR(45);
        
        SET output_param = CONCAT('ID-', input_param);
        SET input_output_param = input_output_param + 1;
    
        SET result_val = CONCAT('Value: ', CAST(input_param AS CHAR));
        RETURN result_val;", returnType: "varchar", returnSize: 45)]
    [RelmProcedureParameter<Groupings>(Groupings.ExampleFunction, CoreRelm.Enums.StoredProcedures.ParameterDirection.Input, "input_param", "bigint", -1)]
    [RelmProcedureParameter<Groupings>(Groupings.ExampleFunction, CoreRelm.Enums.StoredProcedures.ParameterDirection.Output, "output_param", "varchar", 45)]
    [RelmProcedureParameter<Groupings>(Groupings.ExampleFunction, CoreRelm.Enums.StoredProcedures.ParameterDirection.InputOutput, "input_output_param", "int", -1)]

    // Define a stored procedure that takes an input parameter, modifies an output parameter and an input-output parameter, but does not return a value.
    [RelmProcedure<Groupings>(Groupings.ExampleProcedure, "example_procedure", @"
        SET output_param = CONCAT('ID-', input_param);
        SET input_output_param = input_output_param + 1;")]
    [RelmProcedureParameter<Groupings>(Groupings.ExampleProcedure, CoreRelm.Enums.StoredProcedures.ParameterDirection.Input, "input_param", "bigint", -1)]
    [RelmProcedureParameter<Groupings>(Groupings.ExampleProcedure, CoreRelm.Enums.StoredProcedures.ParameterDirection.Output, "output_param", "varchar", 45)]
    [RelmProcedureParameter<Groupings>(Groupings.ExampleProcedure, CoreRelm.Enums.StoredProcedures.ParameterDirection.InputOutput, "input_output_param", "int", -1)]
    internal class ExampleModel : RelmModel
    {
        /*
         * This enum can be defined anywhere in your assembly and is used to group related indexes, functions, and 
         * procedures together for organizational purposes. It has no functional impact on the database schema or 
         * behavior.
        */
        private enum Groupings
        {
            GroupInternalIdModelIndex,
            ExampleFunction,
            ExampleProcedure
        }

        // Define the column defined by the RelmForeignKey below.
        [RelmColumn(columnSize: 45, isNullable: false)]
        [RelmDto]
        public string? GroupInternalId { get; set; } // Column: group_InternalId

        // Define a non-nullable string column.
        [RelmColumn(columnSize: 100, isNullable: false)]
        [RelmDto]
        public string? ModelName { get; set; } // Column: model_name

        // Define an integer column.
        [RelmColumn(columnDbType: MySqlDbType.Int32, defaultValue: "0")]
        [RelmDto]
        public int ModelIndex { get; set; } // Column: model_index

        // Define a boolean column.
        [RelmColumn(columnName: "bool_column", columnDbType: MySqlDbType.Int16, columnSize: 1, defaultValue: "0")]
        [RelmDto]
        public bool IsBoolColumn { get; set; } // Column: bool_column

        // Define a nullable string column that is also a key and is used to link to a modification of this model.
        [RelmColumn(columnSize: 45, isNullable: true)]
        [RelmDto]
        [RelmKey]
        public string? SuperceededByInternalId { get; set; } // Column: superceeded_by_InternalId

        [RelmDataLoader(typeof(IsModificationFieldLoader), keyField: nameof(InternalId))]
        [RelmDto]
        public virtual ExampleModel? ModificationWithModification { get; set; }

        // Define a foreign key relationship to the ExampleGroup model, linking GroupInternalId to ExampleGroup.InternalId.
        [RelmForeignKey(foreignKey: nameof(ExampleGroup.InternalId), localKey: nameof(GroupInternalId))]
        public virtual ExampleGroup? Group { get; set; }

    }
}
