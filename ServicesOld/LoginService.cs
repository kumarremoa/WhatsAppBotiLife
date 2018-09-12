using Infrastructure;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Services
{
    public class LoginService 
    {
        private WAModel _context;

        public LoginService()
        {
            _context = new WAModel();
        }


        public bool ValidateLogin(string userid,string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.userid == userid);
            if(user != null)
            {
                if (user.password.ToLower() == EncryptMD5(password))
                {
                    return true;
                }
            }

            return false;
        }

        public string DecryptMD5(string text)
        {
            var md5 = MD5.Create();
            byte[] result = md5.ComputeHash(Encoding.Default.GetBytes(text));

            var strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
                strBuilder.Append(result[i].ToString("X2"));
            var res = strBuilder.ToString();

            return res.ToLower();
        }

        public string EncryptMD5(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //get hash result after compute it
            byte[] result = md5.Hash;

            var strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits
                //for each byte
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }

    }
}
