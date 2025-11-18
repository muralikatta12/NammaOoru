using System.Security.Cryptography;

namespace NammaOoru.Services
{
    public interface IOtpService
    {
        string GenerateOtp(int length = 6);
        bool ValidateOtpLength(string otp);
    }
    //any class that implements IOtpService must provide implementations for these methods.
    public class OtpService : IOtpService
    {
        public string GenerateOtp(int length = 6)
        {
            byte[] tokenData = new byte[length];
            //byte array to hold random data
            RandomNumberGenerator.Fill(tokenData);
            var digits = "0123456789";
            var otp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                otp += digits[tokenData[i] % 10];
            }
            return otp;
        }

        public bool ValidateOtpLength(string otp)
        {
            return !string.IsNullOrEmpty(otp) && otp.Length == 6 && otp.All(char.IsDigit);
        }
    }
}
