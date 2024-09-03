using System;
using System.Collections.Generic;

class NumberToWordConverter
{
    private static readonly Dictionary<int, string> numberWords = new Dictionary<int, string>
    {
        { 1, "One" },
        { 2, "Two" },
        { 3, "Three" },
        { 4, "Four" },
        { 5, "Five" },
        { 6, "Six" },
        { 7, "Seven" },
        { 8, "Eight" },
        { 9, "Nine" },
        { 10, "Ten" },
        { 11, "Eleven" },
        { 12, "Twelve" },
        { 13, "Thirteen" },
        { 14, "Fourteen" },
        { 15, "Fifteen" },
        { 16, "Sixteen" },
        { 17, "Seventeen" },
        { 18, "Eighteen" },
        { 19, "Nineteen" },
        { 20, "Twenty" },
        { 21, "Twenty-one" },
        { 22, "Twenty-two" },
        { 23, "Twenty-three" },
        { 24, "Twenty-four" },
        { 25, "Twenty-five" },
        { 26, "Twenty-six" },
        { 27, "Twenty-seven" },
        { 28, "Twenty-eight" },
        { 29, "Twenty-nine" }
        // Add more mappings as needed
    };

    public static string Convert(int number)
    {
        if (numberWords.ContainsKey(number))
        {
            return numberWords[number];
        }
        else
        {
            throw new ArgumentOutOfRangeException("Number out of range");
        }
    }

    static void Main(string[] args)
    {
        for (int i = 1; i <= 29; i++)
        {
            Console.WriteLine($"{i} -> {Convert(i)}");
        }
    }
}
