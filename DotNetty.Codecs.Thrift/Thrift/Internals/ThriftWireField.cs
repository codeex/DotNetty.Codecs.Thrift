// Copyright (c) Solal Pirelli
// This code is licensed under the MIT License (see Licence.txt for details)

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ThriftSharp.Internals
{
    /// <summary>
    /// Component of a Thrift struct; can represent either a real field, or a virtual one.
    /// </summary>
    internal sealed class ThriftWireField
    {
        /// <summary>
        /// Gets the field's ID.
        /// </summary>
        public readonly short Id;

        /// <summary>
        /// Gets the field's name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the field's type as used when serializing it to the wire.
        /// </summary>
        public readonly ThriftType WireType;

        /// <summary>
        /// Gets the field's type as specified in code.
        /// </summary>
        public readonly TypeInfo UnderlyingTypeInfo;

        /// <summary>
        /// Gets the field's presence state.
        /// </summary>
        public readonly ThriftWireFieldState Kind;

        /// <summary>
        /// Gets the field's default value, if any.
        /// </summary>
        public readonly object DefaultValue;


        /// <summary>
        /// Gets an expression reading the field, if any.
        /// </summary>
        public readonly Expression Getter;

        /// <summary>
        /// Gets an expression writing its argument to the field, if any.
        /// </summary>
        public readonly Func<Expression, Expression> Setter;

        /// <summary>
        /// Gets an expression testing whether the field's value is null, if any.
        /// </summary>
        public readonly Expression NullChecker;

    }
}