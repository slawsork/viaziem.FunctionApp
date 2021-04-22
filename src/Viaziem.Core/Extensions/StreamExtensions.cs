using System.IO;

namespace Viaziem.Core.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ReadFully(this Stream input)
        {
            using var streamReader = new MemoryStream();
            input.CopyTo(streamReader);
            var result = streamReader.ToArray();

            return result;
        }
    }
}