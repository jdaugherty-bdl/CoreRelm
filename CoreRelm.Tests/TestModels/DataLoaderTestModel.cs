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
    [RelmDataLoader(typeof(DataLoaderTestModelDataLoader))]
    public class DataLoaderTestModel : RelmModel
    {
    }
}
