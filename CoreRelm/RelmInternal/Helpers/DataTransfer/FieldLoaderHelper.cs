using CoreRelm.Interfaces;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class FieldLoaderHelper<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldLoaderHelper{T}"/> class with a single target object.
        /// </summary>
        /// <param name="targetObject">The target object to be used by the helper. Cannot be null.</param>
        public FieldLoaderHelper(T targetObject)
        {
            this.targetObjects = new[] { targetObject };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldLoaderHelper{T}"/> class.
        /// </summary>
        /// <param name="targetObjects">The collection of target objects to be processed. This collection must not be null.</param>
        public FieldLoaderHelper(ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
        }

        /// <summary>
        /// Loads and sets field data for a collection of target objects based on the specified field loader.
        /// </summary>
        /// <remarks>This method retrieves the relevant data for the target objects in the current data
        /// set by using the key fields provided by the <paramref name="fieldLoader"/>. It minimizes database calls by
        /// fetching the data in bulk and then sets the corresponding field values on the target objects.  The method
        /// supports setting fields that are collections (e.g., <see cref="ICollection{T}"/>) or individual values. If
        /// the field to be set is a collection, the method ensures that the data is properly cast to the appropriate
        /// generic type.  If the field specified by the <paramref name="fieldLoader"/> does not exist or is not
        /// compatible with the data type, the field is skipped.</remarks>
        /// <param name="fieldLoader">An implementation of <see cref="IRelmFieldLoaderBase"/> that provides the field data to be loaded. The
        /// <paramref name="fieldLoader"/> must specify the key fields used to identify the data and the name of the
        /// field to be set on the target objects.</param>
        public void LoadData(IRelmFieldLoaderBase fieldLoader)
        {
            // find all fields marked with a RelmFieldLoader attribute that have a type derived from IRelmFieldLoader<> and add them to the list of field loaders as long as they are not already there
            // execute all field loaders
            var referenceKeys = new RelmExecutionCommand().GetReferenceKeys<T>(fieldLoader.KeyFields);

            // get relevant data for items in the current data set all at once to reduce number of database calls
            var fieldData = fieldLoader.GetFieldData(targetObjects.Select(x => x.GetType().GetProperties().Intersect(referenceKeys).Select(y => y.GetValue(x)).ToArray()).ToList());

            // set the relevant field value on all items in the current data set
            foreach (var targetObject in targetObjects)
            {
                var itemValues = targetObject.GetType().GetProperties().Intersect(referenceKeys).Select(y => y.GetValue(targetObject)).ToArray();

                if (fieldData.Keys.Any(x => x.All(y => itemValues.Contains(y))))
                {
                    var fieldValue = fieldData.FirstOrDefault(x => x.Key.All(y => itemValues.Contains(y))).Value;

                    var setField = targetObject.GetType().GetProperty(fieldLoader.FieldName);
                    if (setField != null && setField.PropertyType.IsGenericType && setField.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        var genericType = setField.PropertyType.GetGenericArguments()[0];

                        if (fieldValue is IEnumerable)
                        {
                            var xlist = (fieldValue as IEnumerable)?.Cast<object>()?.ToList();
                            var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(genericType);
                            var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList)).MakeGenericMethod(genericType);
                            var castedList = toListMethod.Invoke(null, new object[] { castMethod.Invoke(null, new object[] { xlist }) });

                            setField.SetValue(targetObject, castedList);
                        }
                        else
                        {
                            setField.SetValue(targetObject, fieldValue);
                        }
                    }
                    else
                    {
                        // Handle cases where setField is not a List<T> or is null
                        setField?.SetValue(targetObject, fieldValue);
                    }
                }
            }
        }
    }
}
