using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.DataTransfer;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class ModelExtensions
    {
        /// <summary>
        /// Loads the results of a foreign key or data loader field into the relevant properties of the supplied target.
        /// </summary>
        /// <typeparam name="T">A RelmModel object type to load the data for.</typeparam>
        /// <typeparam name="R">The field type of the target property.</typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="relmContextOptionsBuilder">The connection options to use when retrieving data.</param>
        /// <param name="target">The object to load the data onto.</param>
        /// <param name="predicate">A member expression indicating which field to load independently.</param>
        /// <param name="customDataLoader"></param>
        /// <param name="additionalConstraints"></param>
        /// <returns>The target object with the relevant data loaded.</returns>
        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader, additionalConstraints);

        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader);

        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, additionalConstraints);

        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate);

        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader, additionalConstraints);

        public static T LoadForeignKeyField<T, R, S>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, customDataLoader);

        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate, additionalConstraints);

        public static T LoadForeignKeyField<T, R>(this T target, IRelmContext relmContext, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => LoadForeignKeyField(target, relmContext.ContextOptions, predicate);

        /// <summary>
        /// Loads the results of a foreign key or data loader field into the relevant properties of the supplied target.
        /// </summary>
        /// <typeparam name="T">A RelmModel object type to load the data for.</typeparam>
        /// <typeparam name="R">The field type of the target property.</typeparam>
        /// <typeparam name="S"></typeparam>
        /// <param name="relmContextOptionsBuilder">The connection options to use when retrieving data.</param>
        /// <param name="target">The object to load the data onto.</param>
        /// <param name="predicate">A member expression indicating which field to load independently.</param>
        /// <param name="customDataLoader"></param>
        /// <param name="additionalConstraints"></param>
        /// <returns>The target object with the relevant data loaded.</returns>
        public static T? LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, R>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader, additionalConstraints).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R, S>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, IRelmDataLoader<S> customDataLoader) where T : IRelmModel, new() where R : IRelmModel, new() where S : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, customDataLoader).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate, Expression<Func<R, object>> additionalConstraints) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate, additionalConstraints).FirstOrDefault();

        public static T? LoadForeignKeyField<T, R>(this T target, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<R>>> predicate) where T : IRelmModel, new() where R : IRelmModel, new()
            => new ForeignKeyLoader<T>(target, relmContextOptionsBuilder).LoadForeignKey(predicate).FirstOrDefault();

        public static T? LoadDataLoaderField<T, S>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(DataLoaderHelper<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, [inputModel]);

            // Get the generic method definition
            var methodInfo = loaderType.GetMethod("LoadField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Could not find LoadField method on {loaderType.Name}");

            // Create a constructed generic method with the S type parameter
            var genericMethod = methodInfo.MakeGenericMethod(typeof(S));

            var loaderResult = genericMethod.Invoke(loaderInstance, [predicate]);

            return ((ICollection<T>)loaderResult).FirstOrDefault();
        }

        public static ICollection<T> LoadDataLoaderField<T, S>(this ICollection<T> inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var relmContext = new RelmContext(relmContextOptionsBuilder);
            var loaderType = typeof(DataLoaderHelper<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { relmContext, inputModel });

            // Get the generic method definition
            var methodInfo = loaderType.GetMethod("LoadField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Could not find LoadField method on {loaderType.Name}");

            // Create a constructed generic method
            var genericMethod = methodInfo.MakeGenericMethod(typeof(S));

            var loaderResult = genericMethod.Invoke(loaderInstance, new object[] { predicate });

            return (ICollection<T>)loaderResult;
        }
    }
}
