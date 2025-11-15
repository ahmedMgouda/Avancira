using System.Text;

namespace Avancira.Infrastructure.Storage;

/// <summary>
/// Detects MIME types from file content by inspecting magic bytes (file signatures)
/// </summary>
public static class MimeTypeDetector
{
    private static readonly Dictionary<string, MimeTypeSignature> _signatures = new()
    {
        // Images
        ["image/jpeg"] = new(new byte[][]
        {
            new byte[] { 0xFF, 0xD8, 0xFF }
        }, ".jpg", ".jpeg"),

        ["image/png"] = new(new byte[][]
        {
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
        }, ".png"),

        ["image/gif"] = new(new byte[][]
        {
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
            new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }  // GIF89a
        }, ".gif"),

        ["image/webp"] = new(new byte[][]
        {
            new byte[] { 0x52, 0x49, 0x46, 0x46 } // RIFF header (check for WEBP later)
        }, ".webp"),

        ["image/bmp"] = new(new byte[][]
        {
            new byte[] { 0x42, 0x4D } // BM
        }, ".bmp"),

        ["image/tiff"] = new(new byte[][]
        {
            new byte[] { 0x49, 0x49, 0x2A, 0x00 }, // Little-endian
            new byte[] { 0x4D, 0x4D, 0x00, 0x2A }  // Big-endian
        }, ".tif", ".tiff"),

        ["image/svg+xml"] = new(new byte[][]
        {
            new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C }, // <?xml
            new byte[] { 0x3C, 0x73, 0x76, 0x67 }        // <svg
        }, ".svg"),

        // Documents
        ["application/pdf"] = new(new byte[][]
        {
            new byte[] { 0x25, 0x50, 0x44, 0x46 } // %PDF
        }, ".pdf"),

        ["application/zip"] = new(new byte[][]
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // PK (used by DOCX, XLSX, etc.)
            new byte[] { 0x50, 0x4B, 0x05, 0x06 },
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }
        }, ".zip", ".docx", ".xlsx", ".pptx"),

        ["application/msword"] = new(new byte[][]
        {
            new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } // OLE2 header
        }, ".doc", ".xls", ".ppt"),

        // Videos
        ["video/mp4"] = new(new byte[][]
        {
            new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, // ftyp
            new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }
        }, ".mp4", ".m4v"),

        ["video/webm"] = new(new byte[][]
        {
            new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } // EBML header
        }, ".webm"),

        ["video/quicktime"] = new(new byte[][]
        {
            new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74 } // ftypqt
        }, ".mov"),

        ["video/x-msvideo"] = new(new byte[][]
        {
            new byte[] { 0x52, 0x49, 0x46, 0x46 } // RIFF (then check for AVI)
        }, ".avi"),

        // Audio
        ["audio/mpeg"] = new(new byte[][]
        {
            new byte[] { 0xFF, 0xFB }, // MP3
            new byte[] { 0xFF, 0xF3 },
            new byte[] { 0xFF, 0xF2 },
            new byte[] { 0x49, 0x44, 0x33 } // ID3
        }, ".mp3"),

        ["audio/wav"] = new(new byte[][]
        {
            new byte[] { 0x52, 0x49, 0x46, 0x46 } // RIFF (check for WAVE)
        }, ".wav"),

        // Text
        ["text/plain"] = new(new byte[][]
        {
            new byte[] { 0xEF, 0xBB, 0xBF } // UTF-8 BOM (optional)
        }, ".txt"),
    };

    /// <summary>
    /// Detects MIME type from file content
    /// </summary>
    /// <param name="stream">File stream (position will be reset)</param>
    /// <returns>Detected MIME type or null if not detected</returns>
    public static async Task<string?> DetectMimeTypeAsync(Stream stream)
    {
        if (stream == null || !stream.CanRead || !stream.CanSeek)
            return null;

        var originalPosition = stream.Position;

        try
        {
            stream.Position = 0;

            // Read first 512 bytes for magic number detection
            var buffer = new byte[512];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
                return null;

            // Check each signature
            foreach (var (mimeType, signature) in _signatures)
            {
                foreach (var magicBytes in signature.MagicBytes)
                {
                    if (bytesRead >= magicBytes.Length && MatchesSignature(buffer, magicBytes))
                    {
                        // Special handling for certain formats
                        if (mimeType == "image/webp")
                        {
                            // Verify it's actually WEBP (check for "WEBP" at offset 8)
                            if (bytesRead >= 12 &&
                                buffer[8] == 0x57 && buffer[9] == 0x45 &&
                                buffer[10] == 0x42 && buffer[11] == 0x50)
                            {
                                return mimeType;
                            }
                        }
                        else if (mimeType == "video/x-msvideo")
                        {
                            // Verify it's AVI (check for "AVI " at offset 8)
                            if (bytesRead >= 12 &&
                                buffer[8] == 0x41 && buffer[9] == 0x56 &&
                                buffer[10] == 0x49 && buffer[11] == 0x20)
                            {
                                return mimeType;
                            }
                        }
                        else if (mimeType == "audio/wav")
                        {
                            // Verify it's WAV (check for "WAVE" at offset 8)
                            if (bytesRead >= 12 &&
                                buffer[8] == 0x57 && buffer[9] == 0x41 &&
                                buffer[10] == 0x56 && buffer[11] == 0x45)
                            {
                                return mimeType;
                            }
                        }
                        else if (mimeType == "image/svg+xml")
                        {
                            // SVG is XML-based, verify it contains svg tags
                            var text = Encoding.UTF8.GetString(buffer, 0, Math.Min(bytesRead, 512));
                            if (text.Contains("<svg", StringComparison.OrdinalIgnoreCase))
                            {
                                return mimeType;
                            }
                        }
                        else
                        {
                            return mimeType;
                        }
                    }
                }
            }

            // Fallback: try to detect text files
            if (IsLikelyTextFile(buffer, bytesRead))
            {
                return "text/plain";
            }

            return null;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Validates that the detected MIME type matches the claimed MIME type
    /// </summary>
    public static async Task<bool> ValidateMimeTypeAsync(Stream stream, string claimedMimeType)
    {
        var detectedMimeType = await DetectMimeTypeAsync(stream);

        if (detectedMimeType == null)
            return false;

        // Exact match
        if (string.Equals(detectedMimeType, claimedMimeType, StringComparison.OrdinalIgnoreCase))
            return true;

        // Handle zip-based formats (DOCX, XLSX are actually ZIP files)
        if (detectedMimeType == "application/zip")
        {
            var zipBasedTypes = new[]
            {
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            };

            if (zipBasedTypes.Contains(claimedMimeType, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        // Handle OLE2-based formats (DOC, XLS are OLE2)
        if (detectedMimeType == "application/msword")
        {
            var ole2Types = new[]
            {
                "application/msword",
                "application/vnd.ms-excel",
                "application/vnd.ms-powerpoint"
            };

            if (ole2Types.Contains(claimedMimeType, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the expected extensions for a MIME type
    /// </summary>
    public static string[] GetExpectedExtensions(string mimeType)
    {
        if (_signatures.TryGetValue(mimeType, out var signature))
        {
            return signature.Extensions;
        }

        return Array.Empty<string>();
    }

    private static bool MatchesSignature(byte[] buffer, byte[] signature)
    {
        if (buffer.Length < signature.Length)
            return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
                return false;
        }

        return true;
    }

    private static bool IsLikelyTextFile(byte[] buffer, int length)
    {
        // Check if the file contains mostly printable ASCII characters
        int printableCount = 0;
        int controlCount = 0;

        for (int i = 0; i < Math.Min(length, 512); i++)
        {
            byte b = buffer[i];

            // Printable ASCII, newline, tab, carriage return
            if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
            {
                printableCount++;
            }
            // Control characters (except whitespace)
            else if (b < 32)
            {
                controlCount++;
            }
        }

        // If more than 95% printable, it's likely text
        return printableCount > 0 && (double)printableCount / length > 0.95;
    }

    private record MimeTypeSignature(byte[][] MagicBytes, params string[] Extensions);
}