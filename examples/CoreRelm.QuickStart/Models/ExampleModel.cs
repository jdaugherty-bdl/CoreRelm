using CoreRelm.Attributes;
using CoreRelm.Models;
using CoreRelm.Quickstart.FieldLoaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Models
{
    [RelmTable("example_models")]
    internal class ExampleModel : RelmModel
    {
        [RelmColumn]
        [RelmDto]
        public string GroupInternalId { get; set; } // Column: group_InternalId

        [RelmColumn]
        [RelmDto]
        public string ModelName { get; set; } // Column: model_name

        [RelmColumn]
        [RelmDto]
        public int ModelIndex { get; set; } // Column: model_index

        [RelmColumn("bool_column")]
        [RelmDto]
        public bool IsBoolColumn { get; set; } // Column: bool_column

        [RelmColumn]
        [RelmDto]
        public string SuperceededByInternalId { get; set; }

        [RelmDto]
        [RelmDataLoader(typeof(IsModificationFieldLoader), keyField: nameof(InternalId))]
        public virtual ExampleModel ModificationWithModification { get; set; }


        [RelmForeignKey(foreignKey: nameof(ExampleGroup.InternalId), localKey: nameof(GroupInternalId))]
        public virtual ExampleGroup Group { get; set; }

    }
}
