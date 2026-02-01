using CoreRelm.Attributes;
using CoreRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.MultipleKeys
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class MultipleKeysTestObject : RelmModel
    {
        public string? MultipleKeysReferenceObjectLocalKey1 { get; set; }
        public string? MultipleKeysReferenceObjectLocalKey2 { get; set; }

        /********************* FOR TESTING REFERENCES *******************************/

        /**** FOREIGN KEY ****/
        public virtual ICollection<MultipleKeysReferenceObject_ForeignKey>? MultipleKeysReferenceObject_ForeignKeys { get; set; }
        public virtual MultipleKeysReferenceObject_ForeignKey? MultipleKeysReferenceObject_ForeignKey_Item { get; set; }

        /**** NAVIGATION PROPERTY ****/
        public virtual ICollection<MultipleKeysReferenceObject_NavigationProperty>? MultipleKeysReferenceObject_NavigationProperties { get; set; }
        public virtual MultipleKeysReferenceObject_NavigationProperty? MultipleKeysReferenceObject_NavigationProperty_Item { get; set; }

        /**** PRINCIPAL ENTITY ****/
        [RelmForeignKey([nameof(MultipleKeysReferenceObject_PrincipalEntity.ReferenceKey1), nameof(MultipleKeysReferenceObject_PrincipalEntity.ReferenceKey2)], [nameof(MultipleKeysReferenceObjectLocalKey1), nameof(MultipleKeysReferenceObjectLocalKey2)])]
        public virtual ICollection<MultipleKeysReferenceObject_PrincipalEntity>? MultipleKeysReferenceObject_PrincipalEntities { get; set; }
        [RelmForeignKey([nameof(MultipleKeysReferenceObject_PrincipalEntity.ReferenceKey1), nameof(MultipleKeysReferenceObject_PrincipalEntity.ReferenceKey2)], [nameof(MultipleKeysReferenceObjectLocalKey1), nameof(MultipleKeysReferenceObjectLocalKey2)])]
        public virtual MultipleKeysReferenceObject_PrincipalEntity? MultipleKeysReferenceObject_PrincipalEntity_Item { get; set; }
    }
}
