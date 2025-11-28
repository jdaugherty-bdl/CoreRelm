using CoreRelm.Attributes;
using CoreRelm.Models;
using CoreRelm.Tests.TestModels.DataLoaderModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class ComplexTestModel : RelmModel
    {
        public enum WhereTypes
        {
            WhereType1,
            WhereType2,
            WhereType3
        }

        [RelmColumn(columnName: "test_column_InternalId", compoundColumnSize: [100, 200], columnSize: 255, isNullable: false, primaryKey: false, autonumber: true, unique: true, defaultValue: "DEFAULTVALUE", index: "INDEX", indexDescending: true, allowDataTruncation: true, isVirtual: true)]
        public string? TestColumnInternalId { get; set; }
        [RelmColumn(columnName: "test_column_id", columnSize: 255, isNullable: false, primaryKey: false, autonumber: true, unique: true, defaultValue: "DEFAULTVALUE", index: "INDEX", indexDescending: true, allowDataTruncation: true, isVirtual: true)]
        public int TestColumnId { get; set; }
        [RelmColumn]
        public string? TestColumnNoAttributeArguments { get; set; }
        [RelmColumn]
        public WhereTypes WhereTypeProperty { get; set; }

        public string? ComplexReferenceObjectLocalKey { get; set; }
        
        public virtual ICollection<ComplexTestModel>? ComplexTestModels { get; set; }

        [RelmDataLoader(typeof(TestFieldBooleanFieldLoader))]
        public virtual bool? TestFieldBoolean { get; set; }

        [RelmDataLoader(typeof(TestFieldBooleansFieldLoader))]
        public virtual ICollection<bool>? TestFieldBooleans { get; set; }

        [RelmDataLoader(typeof(TestFieldStringFieldLoader))]
        public virtual string? TestFieldString { get; set; }
        
        /********************* FOR TESTING REFERENCES *******************************/

        /**** FOREIGN KEY ****/
        public virtual ICollection<ComplexReferenceObject>? ComplexReferenceObjects { get; set; }
        public virtual ComplexReferenceObject? ComplexReferenceObject { get; set; }

        /**** NAVIGATION ENTITY ****/
        public virtual ICollection<ComplexReferenceObject_NavigationProperty>? ComplexReferenceObject_NavigationProperties { get; set; }
        public virtual ComplexReferenceObject_NavigationProperty? ComplexReferenceObject_NavigationPropertyItem { get; set; }

        /**** PRIMARY ENTITY, REMOTE KEY ****/
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelInternalId))]
        public virtual ICollection<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities { get; set; }
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelInternalId))]
        public virtual ComplexReferenceObject_PrincipalEntity? ComplexReferenceObject_PrincipalEntityItem { get; set; }

        /**** PRIMARY ENTITY, LOCAL KEY ****/
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelLocalKey), nameof(ComplexReferenceObjectLocalKey))]
        public virtual ICollection<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities_LocalKeys { get; set; }
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelLocalKey), nameof(ComplexReferenceObjectLocalKey))]
        public virtual ComplexReferenceObject_PrincipalEntity? ComplexReferenceObject_PrincipalEntity_LocalKey { get; set; }

        /**** INVALID REFERENCE ****/
        public virtual ICollection<SimpleReferenceObject>? SimpleReferenceObjects { get; set; }
    }
}
