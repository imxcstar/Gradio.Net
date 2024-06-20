﻿namespace Gradio.Net;

using System.Security.Cryptography;
using System.Text;

internal static class StringExtensions
{
    internal static string ToSnakeCase(this string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (text.Length < 2)
        {
            return text.ToLowerInvariant();
        }

        StringBuilder sb = new();
        sb.Append(char.ToLowerInvariant(text[0]));

        for (int i = 1; i < text.Length; ++i)
        {
            char c = text[i];

            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    internal static string ToMD5_16(this string value)
    {
        using MD5 md5 = MD5.Create();
        return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(value)), 4, 8).Replace("-", "");
    }
}
