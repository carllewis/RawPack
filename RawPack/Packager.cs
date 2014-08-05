// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Packager.cs" company="">
//   Copyright (c) Carl Lewis. All rights reserved.
// </copyright>
// <summary>
//   Packages RAW files such that they may be uploaded to Flickr.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace RawPack
{
    #region Using Directives

    using System;
    using System.IO;

    using ICSharpCode.SharpZipLib.Core;
    using ICSharpCode.SharpZipLib.Zip;

    using ImageMagick;

    #endregion

    /// <summary>
    /// Packages RAW files such that they may be uploaded to Flickr.
    /// </summary>
    public class Packager
    {
        #region Public Methods

        /// <summary>
        /// Creates a package from the RAW file.
        /// </summary>
        /// <param name="sourceFile">
        /// The path to the input file.
        /// </param>
        /// <param name="targetFile">
        /// The (optional) path to the packaged file.
        /// </param>
        /// <returns>
        /// The path to the packaged file.
        /// </returns>
        public string PackageFile(string sourceFile, string targetFile = "")
        {
            var fileToCompress = new FileInfo(sourceFile);

            // create a temporary jpg file from the RAW file
            var jpgFileName = this.CreateThumbnail(fileToCompress);

            // create a zip from the RAW file
            var zipFileName = this.CreateZip(fileToCompress);

            // bind the jpg and zip
            this.CombineFiles(jpgFileName, zipFileName);

            // if a target file was not specified, then put the output in the same folder
            if (string.IsNullOrEmpty(targetFile))
            {
                targetFile = Path.Combine(fileToCompress.DirectoryName ?? string.Empty, fileToCompress.Name + ".jpg");
            }

            // now ensure the output folder exists
            var outputFolder = new DirectoryInfo(new FileInfo(targetFile).Directory.FullName);
            if (!outputFolder.Exists)
            {
                outputFolder.Create();
            }

            File.Move(jpgFileName, targetFile);

            return jpgFileName;
        }

        /// <summary>
        /// The package folder.
        /// </summary>
        /// <param name="sourceFolder">
        /// The source folder.
        /// </param>
        /// <param name="filter">
        /// The filter.
        /// </param>
        /// <param name="recursive">
        /// The recursive.
        /// </param>
        /// <param name="outputFolder">
        /// The output folder.
        /// </param>
        public void PackageFolder(string sourceFolder, string filter, bool recursive, string outputFolder)
        {
            Console.WriteLine("Processing folder: {0}", sourceFolder);
            var sourceFiles = string.IsNullOrEmpty(filter) ? Directory.GetFiles(sourceFolder) : Directory.GetFiles(sourceFolder, filter);
            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    var fileToPackage = new FileInfo(sourceFile);
                    Console.Write(fileToPackage.Name);
                    var outputFileName = Path.Combine(outputFolder, fileToPackage.Name + ".jpg");
                    if (!File.Exists(outputFileName))
                    {
                        this.PackageFile(sourceFile, outputFileName);
                        Console.WriteLine("...Created " + outputFileName);
                    }
                    else
                    {
                        Console.WriteLine("...Package already exists." + outputFileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write("...Failed. ");
                    Console.WriteLine(ex.Message);
                }
            }

            if (recursive)
            {
                foreach (var folder in Directory.GetDirectories(sourceFolder))
                {
                    var childFolder = new DirectoryInfo(folder);
                    this.PackageFolder(folder, filter, true, Path.Combine(outputFolder, childFolder.Name));
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Combines the zip file and the jpg file into a package that can be uploaded to Flickr.
        /// </summary>
        /// <param name="jpgFileName">
        /// The path to the jpg file.
        /// </param>
        /// <param name="zipFileName">
        /// The path to the zip file.
        /// </param>
        private void CombineFiles(string jpgFileName, string zipFileName)
        {
            using (Stream original = new FileStream(jpgFileName, FileMode.Append))
            {
                using (Stream extra = new FileStream(zipFileName, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[32 * 1024];

                    int blockSize;
                    while ((blockSize = extra.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        original.Write(buffer, 0, blockSize);
                    }
                }
            }
        }

        /// <summary>
        /// The create thumbnail.
        /// </summary>
        /// <param name="fileToThumbnail">
        /// The file to create a thumbnail for.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string CreateThumbnail(FileInfo fileToThumbnail)
        {
            // create a jpg file from the RAW file
            var jpgFileName = System.IO.Path.GetTempFileName();

            // mogrify -resample 5 -format jpg img_0690.cr2
            var image = new MagickImage(fileToThumbnail.FullName);
            image.Resample(640, 480);
            image.Format = MagickFormat.Jpg;
            image.Write(jpgFileName);

            return jpgFileName;
        }

        /// <summary>
        /// Compresses the given file into a zip archive.
        /// </summary>
        /// <param name="fileToCompress">
        /// The file to compress.
        /// </param>
        /// <returns>
        /// The name of the created file.
        /// </returns>
        private string CreateZip(FileInfo fileToCompress)
        {
            // create a temporary zip file from the raw file
            var zipFileName = System.IO.Path.GetTempFileName();
            using (var zipStream = new ZipOutputStream(File.Create(zipFileName)))
            {
                zipStream.SetLevel(0);
                zipStream.UseZip64 = UseZip64.Off;
                zipStream.IsStreamOwner = true;

                var newEntry = new ZipEntry(fileToCompress.Name) { DateTime = fileToCompress.LastWriteTime, Size = fileToCompress.Length };
                zipStream.PutNextEntry(newEntry);

                // this writes the contents of the file to compress to the zip file
                var buffer = new byte[4096];
                using (var streamReader = fileToCompress.OpenRead())
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }

                // close the file to compress
                zipStream.CloseEntry();
            }

            return zipFileName;
        }

        #endregion
    }
}