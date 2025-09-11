using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class DataLoaderHelper<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;
        private readonly IRelmQuickContext relmQuickContext;
        private readonly IRelmContext relmContext;

        public DataLoaderHelper(IRelmQuickContext relmContext, T targetObject)
        {
            this.targetObjects = new[] { targetObject };
            this.relmQuickContext = relmContext;
        }

        public DataLoaderHelper(IRelmQuickContext relmContext, ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
            this.relmQuickContext = relmContext;
        }

        public DataLoaderHelper(IRelmContext relmContext, T targetObject)
        {
            this.targetObjects = new[] { targetObject };
            this.relmContext = relmContext;
        }

        public DataLoaderHelper(IRelmContext relmContext, ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
            this.relmContext = relmContext;
        }

        internal ICollection<T> LoadField<R>(Expression<Func<T, R>> predicate)
        {
            var referenceProperty = predicate.Body as MemberExpression
                ?? throw new InvalidOperationException("Collection or property must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var dataLoaderAttribute = referenceProperty.Member.GetCustomAttributes<RelmDataLoader>()
                ?.FirstOrDefault(y => y.LoaderType?.GetInterface(relmContext == null ? nameof(IRelmQuickFieldLoader) : nameof(IRelmFieldLoader)) != null)
                ?? throw new MemberAccessException($"The property or collection [{referenceProperty.Member.Name}] on type [{referenceProperty.Expression.Type.Name}] does not have a RelmDataLoader attribute.");

            var fieldLoader = relmContext == null
                ? (IRelmFieldLoader)Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { relmQuickContext, referenceProperty.Member.Name, dataLoaderAttribute.KeyFields })
                : (IRelmFieldLoader)Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { relmContext, referenceProperty.Member.Name, dataLoaderAttribute.KeyFields });

            new FieldLoaderHelper<T>(targetObjects).LoadData(fieldLoader);

            return targetObjects;
        }
    }
}
