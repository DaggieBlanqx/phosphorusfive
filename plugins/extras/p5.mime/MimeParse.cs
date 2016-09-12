﻿/*
 * Phosphorus Five, copyright 2014 - 2016, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using System.IO;
using p5.exp;
using p5.core;
using p5.exp.exceptions;
using MimeKit;

namespace p5.mime
{
    /// <summary>
    ///     Class wrapping the MIME parse features of Phosphorus Five
    /// </summary>
    public static class MimeParse
    {
        /// <summary>
        ///     Parses the MIME message given as MimeEntity
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.mime.parse-native", Protection = EventProtection.NativeClosed)]
        private static void p5_mime_parse_native (ApplicationContext context, ActiveEventArgs e)
        {
            // Retrieving MimeEntity from caller's arguments
            var entity = e.Args.Get<MimeEntity> (context);
            var parser = new helpers.MimeParser (
                context, 
                e.Args, 
                entity, 
                e.Args.GetExChildValue<string> ("attachment-folder", context));

            // Parses the MimeEntity and stuffs results into e.Args node
            parser.Process ();
        }

        /// <summary>
        ///     Parsess the MIME message given as string
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Active Event arguments</param>
        [ActiveEvent (Name = "p5.mime.parse", Protection = EventProtection.LambdaClosed)]
        public static void p5_mime_parse (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure we clean up after ourselves
            using (new Utilities.ArgsRemover (e.Args, true)) {

                // Looping through each MIME message supplied
                foreach (var idxMimeMessage in XUtil.Iterate<string> (context, e.Args, true)) {

                    // Sanity check
                    if (string.IsNullOrEmpty (idxMimeMessage))
                        throw new LambdaException (
                            "No MIME message provided to [p5.mime.parse]",
                            e.Args,
                            context);

                    // Loading MIME entity from stream
                    using (var writer = new StreamWriter (new MemoryStream ())) {

                        // Writing MIME content to StreamWriter, flushing the stream, and setting reader head back to beginning
                        writer.Write (idxMimeMessage);
                        writer.Flush ();
                        writer.BaseStream.Position = 0;

                        // Loading MimeEntity from MemoryStream
                        MimeEntity entity = null;
                        if (e.Args["Content-Type"] != null)
                            entity = MimeEntity.Load (ContentType.Parse (e.Args["Content-Type"].Get<string> (context)), writer.BaseStream);
                        else
                            entity = MimeEntity.Load (writer.BaseStream);
                        var parser = new helpers.MimeParser (
                            context, 
                            e.Args, 
                            entity, 
                            e.Args.GetExChildValue<string> ("attachment-folder", context));

                        // Parses the MimeEntity and stuffs results into e.Args node
                        parser.Process ();
                    }
                }
            }
        }
    }
}

