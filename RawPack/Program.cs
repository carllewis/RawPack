// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Carl Lewis">
//   Copyright (c) Carl Lewis. All rights reserved.
// </copyright> 
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace RawPack
{
    #region Using Directives 

    using System;
    using System.Collections.Generic;
    using System.IO;

    using NDesk.Options;

    #endregion

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        /// The main method.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        private static void Main(string[] args)
        {
            var mode = "PACK";
            var path = string.Empty;
            var filter = string.Empty;
            var recursive = true;
            var outputPath = string.Empty;
            var showHelp = false;

            var p = new OptionSet
                {
                    { "mode=", "The packaging mode (pack/unpack).", v => mode = v.ToUpper() }, 
                    { "path=", "The path.", v => path = v }, 
                    { "filter=", "The files to package.", v => filter = v }, 
                    { "recursive=", "Whether to include all subfolders.", v => recursive = v != null }, 
                    { "out=", "The output path.", v => outputPath = v }, 
                    { "help", "show this message and exit", v => showHelp = v != null }, 
                };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                WriteErrorMessage(e.Message);
                return;
            }

            if (path.Length == 0)
            {
                if (extra.Count == 0)
                {
                    WriteErrorMessage("You must specify a path.");
                    ShowHelp(p);
                    return;
                }
                
                foreach (var e in extra)
                {
                    if (Directory.Exists(e))
                    {
                        path = e;
                        break;
                    }
                }
            }
            
            if (!Directory.Exists(path))
            {
                WriteErrorMessage("You must specify an existing path.");
                ShowHelp(p);
                return;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = path;
            }
            else if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            if (showHelp)
            {
                ShowHelp(p);
            }

            // everything else is packaging mode
            if (mode == "PACK")
            {
                var packager = new Packager();
                packager.PackageFolder(path, filter, recursive, outputPath);
            }
        }

        /// <summary>
        /// Show the help.
        /// </summary>
        /// <param name="p">
        /// The option set.
        /// </param>
        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("RawPack Version 1.0");
            Console.WriteLine("Copyright (c) Carl Lewis. All rights reserved.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            p.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Writes an error message to the console.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private static void WriteErrorMessage(string message)
        {
            Console.Write("RawPack:");
            Console.WriteLine(message);
            Console.WriteLine("Try  ‘RawPack --help’ for more information.");
        }

        #endregion
    }
}