using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Tests.TestModels.DataLoaderModels
{
    internal class DataLoaderTestModelDataLoader : RelmDefaultDataLoader<DataLoaderTestModel>
    {
        internal override string TableName => "DUMMY NAME";

        public DataLoaderTestModelDataLoader(RelmContextOptionsBuilder contextOptionsBuilder) : base(new RelmContext(contextOptionsBuilder.SetAutoInitializeDataSets(false).SetAutoVerifyTables(false))) { }

        public override ICollection<DataLoaderTestModel?>? PullData(string selectQuery, Dictionary<string, object?> findOptions)
        {
            return
            [
                new() { InternalId = "LOADER1" },
                new() { InternalId = "LOADER2" }
            ];
        }
    }
}
