using Furion.DatabaseAccessor;
using Furion.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Comm
{
    /// <summary>
    /// 分页泛型集合
    /// </summary>
    /// <typeparam name="T">泛型</typeparam>
    /// 
    [SuppressSniffer]
    public class PageListModel<T>
    {
      
        //
        // 摘要:
        //     页码
        public int PageIndex
        {
            get;
            set;
        }

        //
        // 摘要:
        //     页容量
        public int PageSize
        {
            get;
            set;
        }

        //
        // 摘要:
        //     总条数
        public int TotalCount
        {
            get;
            set;
        }

        //
        // 摘要:
        //     总页数
        public int TotalPages
        {
            get;
            set;
        }

        //
        // 摘要:
        //     当前页集合
        public IEnumerable<T> Items
        {
            get;
            set;
        }

    }


    /// <summary>
    /// 分页集合
    /// </summary>
    [SuppressSniffer]
    public class PageListModel : PageListModel<object>
    {

    }
}
