// Generated from GP-λ source
using System;

public static class Program
{
    private static void println(string message)
    {
        Console.WriteLine(message);
    }

    private static string readLine()
    {
        return Console.ReadLine() ?? string.Empty;
    }

    private static string toString(int value)
    {
        return value.ToString();
    }

    private static void main()
    {
        println("Hello, World!");
        string message = "Welcome to GP-λ!";
        println(message);
        int x = 5;
        int y = 10;
        int sum = (x + y);
        println(("5 + 10 = " + toString(sum)));
        System.Diagnostics.Debug.Assert((sum == 15), "Math is broken!");
    }

    public static void Main(string[] args)
    {
        main();
    }
}
