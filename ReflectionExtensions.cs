using System;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Shindr.CSharpLib
{

  /// <summary>
  /// This class contains extension helper method for reflection
  /// </summary>
  public static class ReflectionExtensions
  {

    #region Compiled dynamic functions
    
    /// <summary>
    /// Creates a compiled dynamic type constructor
    /// </summary>
    public static Func<T> GetConstructorFunction<T>(this Type type)
    {
      if (type == null) return null;

      var constructor = Expression.TypeAs(Expression.New(type), typeof(T));
      return (Func<T>)Expression.Lambda(constructor).Compile();
    }


    /// <summary>
    /// Creates a compiled dynamic property getter
    /// </summary>
    public static Func<TObject, TProperty> GetPropertyGetter<TObject, TProperty>(this PropertyInfo propertyInfo)
    {
      var instance = Expression.Parameter(typeof(TObject), "instance");
      var typedInstance = Expression.TypeAs(instance, propertyInfo.DeclaringType);
      var property = Expression.Property(typedInstance, propertyInfo);
      var untypedProperty = Expression.TypeAs(property, typeof(TProperty));
      return (Func<TObject, TProperty>)Expression.Lambda(untypedProperty, instance).Compile();
    }


    /// <summary>
    /// Creates a compiled dynamic property setter
    /// </summary>
    public static Action<TObject, TProperty> GetPropertySetter<TObject, TProperty>(this PropertyInfo propertyInfo)
    {
      var instance = Expression.Parameter(typeof(TObject), "instance");
      var typedInstance = Expression.TypeAs(instance, propertyInfo.DeclaringType);
      var argument = Expression.Parameter(typeof(TProperty), "value");

      var setterCall = Expression.Call(
          typedInstance,
          propertyInfo.GetSetMethod(),
          Expression.Convert(argument, propertyInfo.PropertyType)
          );
      return (Action<TObject, TProperty>)Expression.Lambda(setterCall, instance, argument).Compile();
    }

    #endregion


    /// <summary>
    /// Retrieves a custom attribute of a specified type that is applied to a specified member.
    /// </summary>
    public static T GetAttribute<T>(this MemberInfo memberInfo, bool inherit) where T : Attribute
    {
      object[] attributes = memberInfo.GetCustomAttributes(typeof(T), inherit);
      if ((attributes == null) || (attributes.Length == 0)) return null;

      return attributes[0] as T;
    }


    
    /// <summary>
    /// Returns true if type is numeric
    /// </summary>
    public static bool IsNumeric(this Type type)
    {
      switch (Type.GetTypeCode(type))
      {
        case TypeCode.Byte:
        case TypeCode.Decimal:
        case TypeCode.Double:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.Int64:
        case TypeCode.SByte:
        case TypeCode.Single:
        case TypeCode.UInt16:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          return true;
      }
      return false;
    }


    /// <summary>
    /// Returns true if type implements specific interface
    /// </summary>
    public static bool Implements(this Type type, Type interfaceType)
    {
      return type.GetInterfaces().Contains(interfaceType);
    }


    /// <summary>
    /// Returns parser function if defined for that type
    /// </summary>
    public static Func<string, object> GetParseFunction(this Type type)
    {
      return GetParseFunction(type, CultureInfo.CurrentCulture);
    }


    /// <summary>
    /// Returns Invariant parser function if defined for that type
    /// </summary>
    public static Func<string, object> GetParseInvariantFunction(this Type type)
    {
      return GetParseFunction(type, CultureInfo.InvariantCulture);
    }


    /// <summary>
    /// Returns parser function if defined for that type
    /// </summary>
    public static Func<string, object> GetParseFunction(this Type type, IFormatProvider formatProvider)
    {

      if (type == typeof(Guid))
        return s => Guid.Parse(s);
      else if (type == typeof(TimeSpan))
        return s => TimeSpan.Parse(s, formatProvider);
      else if (type.IsSubclassOf(typeof(Enum)))
        return s => Enum.Parse(type, s, true);

      switch (Type.GetTypeCode(type))
      {
        case TypeCode.Byte:
          return s => byte.Parse(s, formatProvider);
        case TypeCode.SByte:
          return s => sbyte.Parse(s, formatProvider);
        case TypeCode.Int16:
          return s => short.Parse(s, formatProvider);
        case TypeCode.UInt16:
          return s => ushort.Parse(s, formatProvider);
        case TypeCode.Int32:
          return s => int.Parse(s, formatProvider);
        case TypeCode.UInt32:
          return s => uint.Parse(s, formatProvider);
        case TypeCode.Int64:
          return s => long.Parse(s, formatProvider);
        case TypeCode.UInt64:
          return s => ulong.Parse(s, formatProvider);
        case TypeCode.Single:
          return s => float.Parse(s, formatProvider);
        case TypeCode.Double:
          return s => double.Parse(s, formatProvider);
        case TypeCode.Decimal:
          return s => decimal.Parse(s, formatProvider);
        case TypeCode.DateTime:
          return s => DateTime.Parse(s, formatProvider);
      }

      return null;
    }



    /// <summary>
    /// Returns parse exact function if defined for that type
    /// </summary>
    public static Func<string, string, IFormatProvider, object> GetParseExactFunction(this Type type)
    {
      if (type == typeof(DateTime))
        return (s, f, fp) => DateTime.ParseExact(s, f, fp);
      else if (type == typeof(TimeSpan))
        return (s, f, fp) => TimeSpan.ParseExact(s, f, fp);
      else if (type == typeof(Guid))
        return (s, f, fp) => Guid.ParseExact(s, f);

      return null;
    }


    /// <summary>
    /// Returns true if type implements specific interface
    /// </summary>
    public static object GetDefaultValue(this Type type)
    {
      if ((!type.IsValueType) || (type == typeof(Nullable<>)))
        return null;

      if (type == typeof(Guid))
        return default(Guid);
      else if (type == typeof(TimeSpan))
        return default(TimeSpan);
      else if (type.IsSubclassOf(typeof(Enum)))
        return Enum.GetValues(type).GetValue(0);

      switch (Type.GetTypeCode(type))
      {
        case TypeCode.Byte:
          return default(byte);
        case TypeCode.SByte:
          return default(sbyte);
        case TypeCode.Int16:
          return default(short);
        case TypeCode.UInt16:
          return default(ushort);
        case TypeCode.Int32:
          return default(int);
        case TypeCode.UInt32:
          return default(uint);
        case TypeCode.Int64:
          return default(long);
        case TypeCode.UInt64:
          return default(ulong);
        case TypeCode.Single:
          return default(float);
        case TypeCode.Double:
          return default(double);
        case TypeCode.Decimal:
          return default(decimal);
        case TypeCode.DateTime:
          return default(DateTime);
      }

      //use reflection if not found
      return Activator.CreateInstance(type);
    }

  }
}
