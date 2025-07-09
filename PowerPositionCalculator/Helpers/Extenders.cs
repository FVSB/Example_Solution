using ErrorOr;

namespace PowerPositionCalculator;

public static class Extenders
{
    public static string Show(this List<Error> errors)
    {
        var result = "";
        foreach (var error in errors)
        {
            result = $"{result}, {error.ToString()}";
        }

        return result;
    }
}