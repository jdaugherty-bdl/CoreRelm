using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models
{
    public class ForeignKeyNavigationOptions
    {
        public PropertyInfo[] ForeignKeyProperties { get; set; } = default;
        //public PropertyInfo NavigationProperty { get; set; } = default;
        public List<List<Tuple<PropertyInfo, object>>> ItemPrimaryKeys { get; set; } = default;
        public PropertyInfo[] ReferenceKeys { get; set; } = default;

        private MemberExpression _referenceProperty = default;
        public MemberExpression ReferenceProperty
        {
            get
            {
                return _referenceProperty;
            }
            set
            {
                SetReferenceProperty(value);
            }
        }

        public bool IsCollection { get; private set; }
        public Type ReferenceType { get; private set; }

        private void SetReferenceProperty(MemberExpression referenceProperty)
        {
            _referenceProperty = referenceProperty;

            ReferenceType = ReferenceProperty.Type;
            IsCollection = ReferenceType.IsGenericType && ReferenceType.GetGenericTypeDefinition() == typeof(ICollection<>);

            // The type of class being referenced by the collection command
            if (IsCollection)
            {
                ReferenceType = ReferenceProperty.Type.GetGenericArguments()[0];

                // Check if the referenceType is compatible with ICollection<>
                if (!typeof(ICollection<>).MakeGenericType(ReferenceType).IsAssignableFrom(ReferenceProperty.Type))
                    throw new InvalidOperationException($"Reference property type must be compatible with ICollection<{ReferenceType}>.");
            }
            else if (ReferenceType.IsGenericType)
                ReferenceType = ReferenceType.GetGenericArguments().FirstOrDefault();
        }
    }
}
