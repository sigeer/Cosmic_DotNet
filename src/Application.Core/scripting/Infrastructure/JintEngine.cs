using Jint;
using Jint.Runtime.Interop;
using System.Diagnostics.CodeAnalysis;

namespace Application.Core.Scripting.Infrastructure
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

        /// <summary>
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="paramsValue"></param>
        /// <returns>
        /// 不会走 <see cref="CustomeTypeConverter"/>
        /// Array -> object[]
        /// Date -> DateTime
        /// number -> double
        /// string -> string
        /// boolean -> bool
        /// Regex -> RegExp
        /// Function -> Delegate
        /// </returns>
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

        public object GetValue(string variable)
        {
            return _engine.GetValue(variable).ToObject();
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
                converted = arr.ToList();
                return true;
            }

            return base.TryConvert(value, type, formatProvider, out converted);
        }
    }
}
