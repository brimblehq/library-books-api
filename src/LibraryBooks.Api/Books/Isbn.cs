namespace LibraryBooks.Api.Books;

public static class Isbn
{
    public static bool IsValid(string? isbn) => isbn?.Length switch
    {
        10 => IsValidIsbn10(isbn),
        13 => IsValidIsbn13(isbn),
        _ => false,
    };

    static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;
        for (var index = 0; index < 10; index++)
        {
            var character = isbn[index];
            int digit;
            if (char.IsAsciiDigit(character)) digit = character - '0';
            else if (index == 9 && character == 'X') digit = 10;
            else return false;
            sum += digit * (10 - index);
        }
        return sum % 11 == 0;
    }

    static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsAsciiDigit)) return false;

        var sum = 0;
        for (var index = 0; index < 12; index++)
            sum += (isbn[index] - '0') * (index % 2 == 0 ? 1 : 3);

        return (10 - sum % 10) % 10 == isbn[12] - '0';
    }
}
