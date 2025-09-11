using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Utilities
{
    internal class FastActivatorHelper
    {
        internal delegate T ObjectActivator<T>(params object[] args);

        internal static ObjectActivator<T> GetActivator<T>(ConstructorInfo ctor)
        {
            //var type = ctor.DeclaringType;
            var paramsInfo = ctor.GetParameters();

            // create a single param of type object[]
            var param = Expression.Parameter(typeof(object[]), "args");
            var argsExp = new Expression[paramsInfo.Length];

            // pick each arg from the params array and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = paramsInfo[i].ParameterType;
                var paramAccessorExp = Expression.ArrayIndex(param, index);
                var paramCastExp = Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            // make a NewExpression that calls the ctor with the args we just created
            var newExp = Expression.New(ctor, argsExp);

            // create a lambda with the New Expression as body and our param object[] as arg
            var lambda = Expression.Lambda(typeof(ObjectActivator<T>), newExp, param);

            //compile it
            var compiled = (ObjectActivator<T>)lambda.Compile();
            return compiled;
        }
    }
}
