using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AtlasCommerce.Application.Helpers
{
    internal static class MarketPlaceHelper
    {
        public static object[] ToArray(object obj)
        {
            List<object> result = new List<object>();
            result.Add(obj);
            return result.ToArray();
        }
        public static DataTable ConvertArrayToDatatable(object[] arrList)
        {
            DataTable dt = new DataTable();
            try
            {
                if (arrList.Count() > 0)
                {
                    Type arrype = arrList[0].GetType();
                    dt = new DataTable(arrype.Name);

                    foreach (PropertyInfo propInfo in arrype.GetProperties())
                    {
                        dt.Columns.Add(new DataColumn(propInfo.Name));
                    }

                    foreach (object obj in arrList)
                    {
                        DataRow dr = dt.NewRow();

                        foreach (DataColumn dc in dt.Columns)
                        {
                            dr[dc.ColumnName] = obj.GetType().GetProperty(dc.ColumnName).GetValue(obj, null);
                        }
                        dt.Rows.Add(dr);

                    }
                }


                return dt;
            }
            catch (Exception ex)
            {
                return dt;
            }

        }

        public static Int64 DatetimeToLong(DateTime value)
        {
            return System.Convert.ToInt64((value.Subtract(new DateTime(1970, 1, 1))).TotalSeconds) * 1000;
        }

        public static string toBase64String(string username, string password)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
        }

        public static long convertToTimestamp(DateTime value)
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).Ticks / 10000L;
        }

        public static string getMd5Hash(string input)
        {
            byte[] hash = MD5.Create().ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < hash.Length; ++index)
                stringBuilder.Append(hash[index].ToString("x2"));
            return stringBuilder.ToString();
        }

        public static string getSignature(string apiKey, string secretKey, long time)
        {
            string str = (string)null;
            try
            {
                str = getMd5Hash(apiKey + secretKey + (object)time);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return str;
        }
    }
}
