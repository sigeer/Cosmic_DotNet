using Acornima.Ast;

namespace Application.Core.Compatible.Extensions
{
    public static class JsEngineExtensions
    {
        /// <summary>
        /// 兼容js
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int Size(this object list)
        {
            var objType = list.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
                return (int)objType.GetProperty("Count")!.GetValue(list)!;
            if (objType.IsArray && list is Array arr)
                return arr.Length;
            return 0;
        }

        public static object? Get(this object list, int index)
        {
            var objType = list.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var indexer = objType.GetProperty("Item");
                if (indexer != null)
                {
                    var parameters = indexer.GetIndexParameters();
                    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(int))
                    {
                        return indexer.GetValue(list, new object[] { index });
                    }
                }
            }
            if (objType.IsArray && list is Array arr)
            {
                return arr.GetValue(index);
            }
            return null;
        }


        public static bool IsEmpty(this object list)
        {
            return list.Size() == 0;
        }



        //public static object? InvokeSync(this IJsEngine engine, string functionName, params object?[] args)
        //{
        //    var r = engine.CallFunction(functionName, args);
        //    if (r is Task<object> t)
        //    {
        //        return t.Result;
        //    }

        //    return r;
        //}


        //public static TResult? InvokeFunction<TResult>(this IJsEngine engine, string functionName, params object?[] args)
        //{
        //    var r = engine.CallFunction(functionName, args);
        //    if (r is ObjectWrapper jsObject)
        //    {
        //        return (TResult)Convert.ChangeType(jsObject.ToObject(), typeof(TResult));
        //    }
        //    else
        //    {
        //        if (r is TResult d)
        //            return d;

        //        return default;
        //    }
        //}

        //public static List<TResult> InvokeFunctionList<TResult>(this IJsEngine engine, string functionName, params object?[] args)
        //{
        //    var r = engine.CallFunction(functionName, args);
        //    if (r is JsArray arr)
        //    {
        //        return arr.Select(x => (TResult)Convert.ChangeType(x.ToObject(), typeof(TResult))).ToList();
        //    }
        //    return [];
        //}

        ///// <summary>
        ///// js没有返回值时，会返回<see cref="Undefined"/>。
        ///// </summary>
        ///// <param name="engine"></param>
        ///// <param name="functionName"></param>
        ///// <param name="args"></param>
        ///// <returns></returns>

        //public static async Task<object> InvokeAsync(this IJsEngine engine, string functionName, params object[] args)
        //{
        //    var r = engine.CallFunction(functionName, args);
        //    if (r is Task<object> t)
        //    {
        //        return await t;
        //    }

        //    return r;
        //}
    }
}
