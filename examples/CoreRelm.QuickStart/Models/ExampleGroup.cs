using CoreRelm.Attributes;
using CoreRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Quickstart.Models
{
    [RelmTable("example_groups")]
    internal class ExampleGroup : RelmModel
    {
        [RelmColumn]
        [RelmDto]
        public string GroupName { get; set; } // Column: group_name

        [RelmForeignKey(foreignKey: nameof(ExampleModel.GroupInternalId), localKey: nameof(InternalId))]
        public virtual ICollection<ExampleModel> ExampleModels { get; set; }
    }
}
