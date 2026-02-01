using MySql.Data.MySqlClient;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CoreRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver : DefaultRelmResolver_MySQL
    {
        public DefaultRelmResolver(IConfiguration configuration) : base(configuration) { }
    }
}
