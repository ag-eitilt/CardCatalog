﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Metadata.Audio {
    /// <summary>
    /// An implementation of the ID3v2.2 standard as described at
    /// <see href="http://id3.org/id3v2-00"/>
    /// </summary>
    [MetadataFormat(format)]
    public class ID3v22 : ID3v2 {
        /// <summary>
        /// The short name used to represent ID3v2.2 metadata.
        /// </summary>
        /// <seealso cref="MetadataFormat.Register(string, System.Type)"/>
        public const string format = "ID3v2.2";
        /// <summary>
        /// The display name of the tag format.
        /// </summary>
        public override string Format => format;

        /// <summary>
        /// Check whether the stream begins with a valid ID3v2.2 header.
        /// </summary>
        /// <param name="stream">The Stream to check.</param>
        /// <returns>
        /// Whether the stream begins with a valid ID3v2.2 header.
        /// </returns>
        /// <see cref="MetadataFormat.Validate(string, Stream)"/>
        [MetadataFormatValidator]
        public static bool VerifyHeader(Stream stream) {
            return (VerifyBaseHeader(stream)?.Equals(0x02) ?? false);
        }
        /// <summary>
        /// Check whether the byte array begins with a valid ID3v2.2 header.
        /// </summary>
        /// <param name="header">The byte array to check</param>
        /// <returns>
        /// `null` if the stream does not begin with a ID3v2.2 header, and the
        /// major version if it does.
        /// </returns>
        public static bool VerifyHeader(byte[] header) {
            return (VerifyBaseHeader(header)?.Equals(0x02) ?? false);
        }

        /// <summary>
        /// The underlying low-level tag data.
        /// </summary>
        /// 
        /// <seealso cref="Fields"/>
        private Dictionary<byte[], ITagField> fields = new Dictionary<byte[], ITagField>();
        /// <summary>
        /// The low-level representations of the tag data.
        /// </summary>
        public override IReadOnlyDictionary<byte[], ITagField> Fields => fields;

        /// <summary>
        /// Implement the audio field attribute mappings for ID3v2.2 tags.
        /// </summary>
        class AttributeStruct : AudioTagAttributes {
            private ID3v22 parent;

            public AttributeStruct(ID3v22 parent) {
                this.parent = parent;
            }

            public override string Name => throw new NotImplementedException();
        }
        /// <summary>
        /// Retrieve the audio field attribute mappings for ID3v2.2 tags.
        /// </summary>
        /// 
        /// <seealso cref="Fields"/>
        public override AudioTagAttributes Attributes => new AttributeStruct(this);

        /// <summary>
        /// Parse a stream according the proper version of the ID3v2
        /// specification, from the current location.
        /// </summary>
        /// <remarks>
        /// As according to the recommendation in the ID3v2.2 specification,
        /// if the tag is compressed, it is swallowed but largely ignored.
        /// </remarks>
        /// <param name="stream">The stream to parse.</param>
        /// <seealso cref="MetadataFormat.Construct(string, Stream)"/>
        public ID3v22(Stream stream) {
            byte[] tag = ParseHeaderAsync(stream).Result;
        }

        /// <summary>
        /// Extract and encapsulate the code used to parse a ID3v2 header into
        /// usable variables, and use that to retrieve the rest of the tag.
        /// </summary>
        /// <param name="stream">The stream to parse.</param>
        /// <returns>
        /// The remainder of the tag, properly de-unsynchronized.
        /// </returns>
        async Task<byte[]> ParseHeaderAsync(Stream stream) {
            var header = ParseBaseHeader(stream, VerifyHeader);

            bool useUnsync = header.Item1[0];
            // flags[1] is handled below
            /*TODO: May be better to skip reading the tag rather than setting
             * FlagUnknown, as these flags tend to be critical to the proper
             * parsing of the tag.
             */
            FlagUnknown = (header.Item1.Cast<bool>().Skip(2).Contains(true));

            // ID3v2.2 uses this flag to indicate compression, but recommends
            // ignoring the tag if it's set
            if (header.Item1[1]) {
                stream.Position += header.Item2;
                return new byte[0];
            } else
                return await ReadBytesAsync(stream, header.Item2, useUnsync).ConfigureAwait(false);
        }
    }
}