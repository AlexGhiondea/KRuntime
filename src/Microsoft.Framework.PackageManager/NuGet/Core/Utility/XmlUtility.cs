// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NuGet
{
    internal static class XmlUtility
    {
        public static XDocument GetOrCreateDocument(XName rootName, IFileSystem fileSystem, string path)
        {
            if (fileSystem.FileExists(path))
            {
                try
                {
                    return GetDocument(fileSystem, path);
                }
                catch (FileNotFoundException)
                {
                    return CreateDocument(rootName, fileSystem, path);
                }
            }
            return CreateDocument(rootName, fileSystem, path);
        }

        private static XDocument LoadSafe(Stream input, LoadOptions options)
        {
            var settings = CreateSafeSettings();
            var reader = XmlReader.Create(input, settings);
            return XDocument.Load(reader, options);
        }

        private static XDocument CreateDocument(XName rootName, IFileSystem fileSystem, string path)
        {
            XDocument document = new XDocument(new XElement(rootName));
            // Add it to the file system
            fileSystem.AddFile(path, document.Save);
            return document;
        }

        private static XDocument GetDocument(IFileSystem fileSystem, string path)
        {
            using (Stream configStream = fileSystem.OpenFile(path))
            {
                return LoadSafe(configStream, LoadOptions.PreserveWhitespace);
            }
        }

        private static XmlReaderSettings CreateSafeSettings(bool ignoreWhiteSpace = false)
        {
            var safeSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreWhitespace = ignoreWhiteSpace
            };

            return safeSettings;
        }
    }
}
