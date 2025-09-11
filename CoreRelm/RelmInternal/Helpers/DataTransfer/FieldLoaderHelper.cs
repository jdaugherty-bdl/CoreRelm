using CoreRelm.Interfaces;
using CoreRelm.Models;
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

        public FieldLoaderHelper(T targetObject)
        {
            this.targetObjects = new[] { targetObject };
        }

        public FieldLoaderHelper(ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
        }

        public void LoadData(IRelmFieldLoader fieldLoader)
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
