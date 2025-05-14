using System.Text;

namespace Kedo.Comm
{
    public static class ByteUtil
    {
        public static string ByteToStr(byte[] byteArray)
        {
            StringBuilder strDigest = new StringBuilder();
            for (int i = 0; i < byteArray.Length; i++)
            {
                strDigest.Append(ByteToHexStr(byteArray[i]));
            }
            return strDigest.ToString();
        }
        private static string ByteToHexStr(byte mByte)
        {
            char[] Digit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            char[] tempArr = new char[2];
            tempArr[0] = Digit[(mByte >> 4) & 0X0F];
            tempArr[1] = Digit[mByte & 0X0F];
            string s = new string(tempArr);
            return s;
        }
    }
}
