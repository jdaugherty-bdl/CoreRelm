using CoreRelm.Attributes;
using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CoreRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignKeyLoader<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;
        private readonly RelmContextOptionsBuilder contextOptions;

        public ForeignKeyLoader(T targetObject, RelmContextOptionsBuilder contextOptions)
        {
            if (targetObject == null)
                throw new ArgumentNullException(nameof(targetObject));

            this.targetObjects = new[] { targetObject };
            this.contextOptions = contextOptions;
        }

        public ForeignKeyLoader(ICollection<T> targetObjects, RelmContextOptionsBuilder contextOptions)
        {
            if (targetObjects == null)
                throw new ArgumentNullException(nameof(targetObjects));

            this.targetObjects = targetObjects;
            this.contextOptions = contextOptions;
        }

        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, R>> predicate) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), null);
        }

        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), additionalConstraints);
        }

        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKey(predicate, customDataLoader, null);
        }

        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKey(predicate, customDataLoader, null);
        }

        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, ICollection<R>>> predicate) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), null);
        }

        internal ICollection<T> LoadForeignKey<R>(Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new()
        {
            return LoadForeignKey(predicate, default(IRelmDataLoader<RelmModel>), additionalConstraints);
        }

        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKeyInternal(predicate, customDataLoader, additionalConstraints);
        }

        internal ICollection<T> LoadForeignKey<R, S>(Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            return LoadForeignKeyInternal(predicate, customDataLoader, additionalConstraints);
        }

        private ICollection<T> LoadForeignKeyInternal<R, S>(Expression predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where R : IRelmModel, new() where S : IRelmModel, new()
        {
            if ((targetObjects?.Count ?? 0) == 0)
                return null;

            // get all types in the context assembly and look for one that inherits from RelmContext
            //var member = predicate.Body;
            var member = (predicate as Expression<Func<T, R>>)?.Body ?? (predicate as Expression<Func<T, ICollection<R>>>)?.Body;
            var referenceProperty = member as MemberExpression
                ?? throw new InvalidOperationException("Collection or property must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var referenceType = referenceProperty.Type;

            if (referenceProperty.Member.GetCustomAttribute<RelmDataLoader>() != null)
                throw new CustomAttributeFormatException($"Field [{referenceType.Name}] is a Data Loader field, not a Foreign Key field");

            var relevantContext = Assembly
                .GetAssembly(typeof(T))
                .GetTypes()
                .Where(x => x.BaseType == typeof(RelmContext))
                .FirstOrDefault(x => x
                    .GetProperties()
                    .Where(y => y.PropertyType == typeof(IRelmDataSet<T>))
                    .Any());

            var relevantDataSet = relevantContext.GetProperties().FirstOrDefault(x => x.PropertyType == typeof(IRelmDataSet<T>));

            var relevantProperty = relevantDataSet.PropertyType.GetGenericArguments().FirstOrDefault().GetProperties().FirstOrDefault(x => x.PropertyType == typeof(T))
                ?? relevantDataSet.PropertyType.GetGenericArguments().FirstOrDefault().GetProperties().FirstOrDefault(x => x.PropertyType.GenericTypeArguments.Any(y => y == typeof(T)));

            //var currentContext = (IRelmContext)Activator.CreateInstance(relevantContext, new object[] { contextOptions });
            var contextConstructor = relevantContext.GetConstructor(new Type[] { typeof(RelmContextOptionsBuilder) });
            var contextActivator = FastActivatorHelper.GetActivator<IRelmContext>(contextConstructor);
            var currentContext = contextActivator(contextOptions);

            if (customDataLoader != null)
            {
                //var returnType = predicate.ReturnType;
                var returnType = (predicate as Expression<Func<T, R>>)?.ReturnType ?? (predicate as Expression<Func<T, ICollection<R>>>)?.ReturnType;
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    returnType = returnType.GetGenericArguments()[0];

                var foreignDataSet = relevantContext.GetProperties().FirstOrDefault(x => x.PropertyType == typeof(IRelmDataSet<>).MakeGenericType(returnType));

                foreignDataSet
                    .PropertyType
                    .GetMethod(nameof(IRelmDataSet<T>.SetDataLoader))
                    .Invoke(foreignDataSet.GetValue(currentContext), new object[] { customDataLoader });
            }

            //var executionCommand = new RelmExecutionCommand(Command.Reference, predicate.Body);
            var executionCommand = new RelmExecutionCommand(Command.Reference, member);

            if (additionalConstraints != null)
                executionCommand.AddAdditionalCommand(Command.Reference, additionalConstraints.Body);

            var objectsLoader = new ForeignObjectsLoader<T>(targetObjects, currentContext);
            objectsLoader.LoadForeignObjects(executionCommand);

            return targetObjects;
        }
    }
}
