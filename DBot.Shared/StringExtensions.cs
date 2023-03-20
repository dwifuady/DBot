namespace DBot.Shared;

public static class StringExtensions
{
    public static T Parse<T>(this string s) where T : IParsable<T> {
      return T.Parse(s, null);
   }
}
