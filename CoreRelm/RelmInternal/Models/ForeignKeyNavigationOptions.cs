using MoreLinq;
using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models
{
    /// <summary>
    /// Class to hold foreign key navigation options, used internally by CoreRelm to manage relationships between models.
    /// </summary>
    public class ForeignKeyNavigationOptions
    {
        internal PropertyInfo[]? ForeignKeyProperties { get; set; } = default;
        //public PropertyInfo NavigationProperty { get; set; } = default;
        internal List<List<Tuple<PropertyInfo, object>>>? ItemPrimaryKeys { get; set; } = default;
        internal PropertyInfo[]? ReferenceKeys { get; set; } = default;

        private MemberExpression? _referenceProperty = default;
        internal MemberExpression? ReferenceProperty
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

        internal bool IsCollection { get; private set; }
        internal Type? ReferenceType { get; private set; }

        private void SetReferenceProperty(MemberExpression? referenceProperty)
        {
            if (referenceProperty == null)
            {
                _referenceProperty = null;
                ReferenceType = null;
                IsCollection = false;
                return;
            }

            _referenceProperty = referenceProperty;

            ReferenceType = _referenceProperty.Type;
            IsCollection = ReferenceType?.IsGenericType == true && ReferenceType.GetGenericTypeDefinition() == typeof(ICollection<>);

            // The type of class being referenced by the collection command
            if (IsCollection)
            {
                ReferenceType = _referenceProperty.Type.GetGenericArguments()[0];

                // Check if the referenceType is compatible with ICollection<>
                if (!typeof(ICollection<>).MakeGenericType(ReferenceType).IsAssignableFrom(_referenceProperty.Type))
                    throw new InvalidOperationException($"Reference property type must be compatible with ICollection<{ReferenceType}>.");
            }
            else if (ReferenceType!.IsGenericType)
                ReferenceType = ReferenceType.GetGenericArguments().FirstOrDefault();
        }
    }
}
