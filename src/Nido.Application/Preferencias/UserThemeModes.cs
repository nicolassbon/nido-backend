namespace Nido.Application.Preferencias;

public static class UserThemeModes
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string System = "system";

    public static bool IsValid(string value)
        => value is Light or Dark or System;
}
