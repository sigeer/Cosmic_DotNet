﻿using Application.Core.Scripting.Infrastructure;
using Jint;
using Jint.Runtime.Interop;
using System.Diagnostics.CodeAnalysis;

namespace Application.Core.scripting.Infrastructure
{
    public class JintEngine : IEngine
    {
        readonly Engine _engine;
        public JintEngine()
        {
            _engine = new Engine(o =>
            {
                o.AllowClr().AddExtensionMethods(typeof(JsEngineExtensions));
                o.SetTypeConverter(o =>
                {
                    return new CustomeTypeConverter(o);
                });
                o.Strict = false;
            });
        }
        public void AddHostedObject(string name, object obj)
        {
            _engine.SetValue(name, obj);
        }

        public void AddHostedType(string name, Type type)
        {
            _engine.SetValue(name, type);
        }

        public object? CallFunction(string functionName, params object?[] paramsValue)
        {
            var m = _engine.Invoke(functionName, paramsValue);
            return m.ToObject();
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        public object Evaluate(string code)
        {
            return _engine.Evaluate(code);
        }
    }

    public class CustomeTypeConverter : DefaultTypeConverter
    {
        public CustomeTypeConverter(Engine engine) : base(engine)
        {
        }

        public override bool TryConvert(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)] Type type, IFormatProvider formatProvider, [NotNullWhen(true)] out object? converted)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && value is object[] arr)
            {
                var list = new List<object>();
                foreach (var element in arr)
                {
                    list.Add(element);
                }
                converted = list;
                return true;
            }

            return base.TryConvert(value, type, formatProvider, out converted);
        }
    }
}
