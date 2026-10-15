using System.ComponentModel;
using System.Diagnostics;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// Runs GDAL's ogr2ogr: the machine's own when it has one, and GDAL's Docker image when not. Every
/// path passed in is absolute, and Docker sees each folder at the same path, so the arguments need
/// no change between the two.
/// </summary>
public static class Gdal
{
    /// <summary>GDAL's own image, for a machine with no ogr2ogr of its own; the tag fixes the version.</summary>
    public const string Image = "ghcr.io/osgeo/gdal:alpine-small-3.11.4";

    private static readonly Lazy<bool> Local = new(() => Start("ogr2ogr", ["--version"]).Result.IsSuccess);

    /// <summary>
    /// Runs ogr2ogr with <paramref name="arguments"/> and returns what it wrote to its output.
    /// <paramref name="folders"/> are the folders Docker must see, when it runs the image.
    /// </summary>
    public static Task<Result<string, string>> Run(IReadOnlyList<string> arguments, IReadOnlyList<string> folders) =>
        Local.Value
            ? Start("ogr2ogr", arguments)
            : Start("docker", ["run", "--rm", .. folders.Distinct().SelectMany(folder => new[] { "--volume", $"{folder}:{folder}" }), Image, "ogr2ogr", .. arguments]);

    // Runs one program to its end. The output is read as it comes, so a large table cannot fill
    // the pipe and leave the program waiting for a reader.
    private static async Task<Result<string, string>> Start(string program, IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode == 0
                ? Result.Success<string, string>(await output.ConfigureAwait(false))
                : Result.Failure<string, string>($"{program} failed: {(await errors.ConfigureAwait(false)).Trim()}");
        }
        catch (Win32Exception)
        {
            return Result.Failure<string, string>($"{program} is not installed.");
        }
    }
}
