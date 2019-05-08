using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// come from thrify,
/// 此处和我设想不太一样，最终目标不需要这些标签
/// </summary>
namespace ThriftAnnotations.Annotations
{
    /// <summary>
    /// 标识一个 Thrift 结构的序列化时所使用的构造函数。
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class ThriftConstructorAttribute : Attribute
    {

    }
}
