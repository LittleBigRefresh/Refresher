using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace Refresher.CLI;

#nullable disable

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class CommandLineOptions
{
    [Option('i', "input", Required = true, HelpText = "The input EBOOT.elf to patch")]
    public string InputFile { get; set; }
        
    [Option('u', "url", Required = true, HelpText = "The URL to patch to")]
    public string ServerUrl { get; set; }
    
    [Option('d', "digest", Required = false, HelpText = "Whether or not to patch the digest key")]
    public bool? Digest { get; set; }
        
    [Option('o', "output", Required = true, HelpText = "The output EBOOT.elf to save")]
    public string OutputFile { get; set; }
}