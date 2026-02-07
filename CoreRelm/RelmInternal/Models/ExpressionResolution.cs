using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models
{
    internal class ExpressionResolution
    {
        public string? Query { get; set; }

        public string? TableAlias { get; set; }

        public string? FieldName { get; set; }

        public string? ParameterName { get; set; }

        public object? ParameterValue { get; set; }

        public ExpressionType NodeType { get; set; }

        public ExpressionResolution Clone()
        {
            var newResolution = new ExpressionResolution
            {
                Query = this.Query,
                FieldName = this.FieldName,
                TableAlias = this.TableAlias,
                ParameterName = this.ParameterName,
                ParameterValue = this.ParameterValue
            };

            return newResolution;
        }
    }
}
