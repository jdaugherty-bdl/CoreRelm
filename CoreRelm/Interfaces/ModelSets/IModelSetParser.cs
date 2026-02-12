using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.ModelSets
{
    public interface IModelSetParser
    {
        ModelSetsFile Parse(string json);
    }
}
