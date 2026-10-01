using System.Security.Cryptography;

namespace ClinicManagementSystem.Utilities;

public static class TemporaryPasswordGenerator
{
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnpqrstuvwxyz";
    private const string DigitChars = "23456789";

    public static string Generate()
    {
        var chars = new[]
        {
            Pick(UpperChars), Pick(LowerChars), Pick(DigitChars), Pick(LowerChars),
            Pick(UpperChars), Pick(DigitChars), Pick(LowerChars), Pick(DigitChars)
        };

        return new string(chars);
    }

    private static char Pick(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
}