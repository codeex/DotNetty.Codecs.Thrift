// Copyright (c) CodeEx.cn & webmote. All rights reserved.
// Licensed under theApache License. 
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetty.Codecs.Thrift
{
    /// <summary>
    /// 解析Thrift需设置的参数
    /// </summary>
    public class ThriftOptions
    {
        public static ThriftOptions DefaultOptions = new ThriftOptions();
        /// <summary>
        /// 解析数据时，最大递归深度 64
        /// </summary>
        public const int DefaultRecursionDepth = 64;
        //todo： 以后如果有变动
        public ThriftOptions(string thriftVersion = "0.9")
        {
            VERSION_MASK = 0xffff0000;
            VERSION_1 = 0x80010000;
            MaxFrameSize = 10240 * 1024; //10M
            StrictRead = false;
            StrictWrite = true;
        }
        /// <summary>
        /// 最大通讯字节限制
        /// </summary>
        public int MaxFrameSize { get; set; }
        /// <summary>
        /// 严格读
        /// </summary>
        public bool StrictRead { get; set; }
        /// <summary>
        /// 严格写
        /// </summary>
        public bool StrictWrite { get; set; }

        /// <summary>
        /// 版本标识
        /// </summary>
        public readonly uint VERSION_1;

        /// <summary>
        /// 版本掩码
        /// </summary>

        public readonly uint VERSION_MASK;

       
    }
}
