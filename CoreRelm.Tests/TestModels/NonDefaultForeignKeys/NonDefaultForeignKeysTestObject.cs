using CoreRelm.Attributes;
using CoreRelm.Models;
using CoreRelm.Tests.TestModels.MultipleKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.NonDefaultForeignKeys
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class NonDefaultForeignKeysTestObject : RelmModel
    {
        public string? NonDefaultForeignKeysReferenceObjectLocalKey { get; set; }

        /********************* FOR TESTING REFERENCES *******************************/

        /**** FOREIGN KEY ****/
        public virtual ICollection<NonDefaultForeignKeysReferenceObject_ForeignKey>? NonDefaultForeignKeysReferenceObject_ForeignKeys { get; set; }
        public virtual NonDefaultForeignKeysReferenceObject_ForeignKey? NonDefaultForeignKeysReferenceObject_ForeignKey_Item { get; set; }

        /**** NAVIGATION PROPERTY ****/
        public virtual ICollection<NonDefaultForeignKeysReferenceObject_NavigationProperty>? NonDefaultForeignKeysReferenceObject_NavigationProperties { get; set; }
        public virtual NonDefaultForeignKeysReferenceObject_NavigationProperty? NonDefaultForeignKeysReferenceObject_NavigationProperty_Item { get; set; }

        /**** PRINCIPAL ENTITY ****/
        [RelmForeignKey(nameof(NonDefaultForeignKeysReferenceObject_PrincipalEntity.ReferenceKey), nameof(NonDefaultForeignKeysReferenceObjectLocalKey))]
        public virtual ICollection<NonDefaultForeignKeysReferenceObject_PrincipalEntity>? NonDefaultForeignKeysReferenceObject_PrincipalEntities { get; set; }
        [RelmForeignKey(nameof(NonDefaultForeignKeysReferenceObject_PrincipalEntity.ReferenceKey), nameof(NonDefaultForeignKeysReferenceObjectLocalKey))]
        public virtual NonDefaultForeignKeysReferenceObject_PrincipalEntity? NonDefaultForeignKeysReferenceObject_PrincipalEntity_Item { get; set; }
    }
}
