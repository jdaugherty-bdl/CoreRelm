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

        public DataLoaderTestModelDataLoader(RelmContextOptionsBuilder contextOptionsBuilder) : base(contextOptionsBuilder) { }

        public override ICollection<DataLoaderTestModel> PullData(string selectQuery, Dictionary<string, object> findOptions)
        {
            return new List<DataLoaderTestModel>
            {
                new DataLoaderTestModel { InternalId = "LOADER1" },
                new DataLoaderTestModel { InternalId = "LOADER2" }
            };
        }
    }
}
