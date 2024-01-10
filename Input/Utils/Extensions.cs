namespace DiscJockey.Input.Utils;

public static class Extensions
{
    public static string RawKeyFromInputBinding(this string inputBinding)
    {
        var bindingSplit = inputBinding.Split('/');
        // Input Actions should have a /, but lets check to be sure.
        return bindingSplit.Length == 1 ? bindingSplit[0] : bindingSplit[1];
    }
}