using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Kedo.Core.Method
{
    public class CreateRandomCode
    {        /// <summary>
        /// 创建订阅随机码
        /// </summary>
        /// <returns></returns>
        public static string CreateCode()
        {
            RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
            byte[] byteCsp = new byte[10];
            csp.GetBytes(byteCsp);
            return BitConverter.ToString(byteCsp);
        }


        public static string CreateNo()
        {
            Random random = new Random();
            string strRandom = random.Next(1000, 10000).ToString(); //生成编号 
            string code = DateTime.Now.ToString("yyyyMMddHHmmss") + strRandom;//形如
            return code;
        }

    }
}
