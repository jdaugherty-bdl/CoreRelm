using CoreRelm.Attributes;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreRelm.Tests.RelmInternal.Helpers.Operations
{
    public class DataNamingHelper_Tests
    {
        [Fact]
        public void GetUnderscoreProperties_ByType_ResolvesNamesAndFiltersCorrectly()
        {
            // Arrange
            var targetType = typeof(DataNamingHelperModel);

            // Act
            var results = DataNamingHelper.GetUnderscoreProperties(targetType);
            var resultMap = results.ToDictionary(x => x.Key, x => x.Value);

            // Assert
            Assert.Contains("InternalId", resultMap.Keys);
            Assert.Contains("Group_InternalId", resultMap.Keys);
            Assert.Contains("Test_Group_InternalId", resultMap.Keys);
            Assert.Contains("InternalId_Value", resultMap.Keys);
            Assert.Contains("Sample_Name", resultMap.Keys);
            Assert.Contains("custom_column", resultMap.Keys);
            Assert.Contains("Checksum_256", resultMap.Keys);
            Assert.Contains("Checksum_256_Calculated", resultMap.Keys);
            Assert.DoesNotContain("Virtual_Value", resultMap.Keys);
            Assert.DoesNotContain("NoAttribute", resultMap.Keys);

            Assert.Contains("Test_Value", resultMap.Keys);
            Assert.Contains("Test_Value_0", resultMap.Keys);

            var duplicateNames = new HashSet<string>
            {
                resultMap["Test_Value"].Item1,
                resultMap["Test_Value_0"].Item1,
            };

            Assert.True(duplicateNames.SetEquals(["Test_Value"]));
        }

        [Fact]
        public void GetUnderscoreProperties_IncludingVirtualColumns_ReturnsVirtualColumn()
        {
            // Arrange
            var targetType = typeof(DataNamingHelperModel);

            // Act
            var results = DataNamingHelper.GetUnderscoreProperties(targetType, GetOnlyDbResolvables: true, GetOnlyNonVirtualColumns: false);
            var resultMap = results.ToDictionary(x => x.Key, x => x.Value);

            // Assert
            Assert.Contains("Virtual_Value", resultMap.Keys);
        }

        [Fact]
        public void GetUnderscoreProperties_IncludeAllWhenDbResolvablesFalse()
        {
            // Arrange
            var targetType = typeof(DataNamingHelperAllColumnsModel);
            var expectedPropertyCount = targetType.GetProperties().Length;

            // Act
            var results = DataNamingHelper.GetUnderscoreProperties(targetType, GetOnlyDbResolvables: false);
            var resultMap = results.ToDictionary(x => x.Key, x => x.Value);

            // Assert
            Assert.Equal(expectedPropertyCount, resultMap.Count);
            Assert.Contains("First_Value", resultMap.Keys);
            Assert.Contains("trimmed_column", resultMap.Keys);
            Assert.Contains("Second_Value", resultMap.Keys);
        }

        [Fact]
        public void GetUnderscoreProperties_Overloads_ReturnEquivalentResults()
        {
            // Arrange
            var instance = new DataNamingHelperAllColumnsModel();

            // Act
            var fromObject = DataNamingHelper.GetUnderscoreProperties(instance);
            var fromGeneric = DataNamingHelper.GetUnderscoreProperties<DataNamingHelperAllColumnsModel>();
            var fromType = DataNamingHelper.GetUnderscoreProperties(typeof(DataNamingHelperAllColumnsModel));

            var objectKeys = new HashSet<string>(fromObject.Select(x => x.Key));
            var genericKeys = new HashSet<string>(fromGeneric.Select(x => x.Key));
            var typeKeys = new HashSet<string>(fromType.Select(x => x.Key));

            // Assert
            Assert.True(objectKeys.SetEquals(genericKeys));
            Assert.True(objectKeys.SetEquals(typeKeys));
        }

        private class DataNamingHelperModel
        {
            [RelmColumn]
            public string? InternalId { get; set; }

            [RelmColumn]
            public string? GroupInternalId { get; set; }

            [RelmColumn]
            public string? TestGroupInternalId { get; set; }

            [RelmColumn]
            public string? InternalIdValue { get; set; }

            [RelmColumn]
            public string? SampleName { get; set; }

            [RelmColumn(columnName: " custom_column ")]
            public string? CustomNamed { get; set; }

            [RelmColumn(isVirtual: true)]
            public string? VirtualValue { get; set; }

            [RelmColumn]
            public string? TestValue { get; set; }

            [RelmColumn]
            public string? Test_Value { get; set; }

            [RelmColumn]
            public string? Checksum256 { get; set; }

            [RelmColumn]
            public string? Checksum256Calculated { get; set; }

            public string? NoAttribute { get; set; }
        }

        private class DataNamingHelperAllColumnsModel
        {
            [RelmColumn]
            public string? FirstValue { get; set; }

            [RelmColumn(columnName: "  trimmed_column  ")]
            public string? TrimmedColumn { get; set; }

            [RelmColumn]
            public int? SecondValue { get; set; }
        }
    }
}