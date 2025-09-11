using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public class RelmDataLoader : Attribute
    {
        public Type LoaderType { get; set; } = default;
        public string[] KeyFields { get; set; } = default;

        public RelmDataLoader(Type LoaderType)
        {
            this.LoaderType = LoaderType;
        }

        public RelmDataLoader(Type LoaderType, string KeyField)
        {
            this.LoaderType = LoaderType;

            if (KeyField != null)
                this.KeyFields = [KeyField];
        }

        public RelmDataLoader(Type LoaderType, string[] KeyFields)
        {
            this.LoaderType = LoaderType;
            this.KeyFields = KeyFields;
        }
    }
}
