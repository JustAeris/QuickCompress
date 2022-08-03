using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;
// ReSharper disable MemberCanBePrivate.Global

namespace QuickCompress.Core.FileCompression;

public static class Zip
{
    public static void CompressFiles(FileInfo fileToCompress, string destination) => CompressFiles(new []{fileToCompress}, destination);

    public static void CompressFiles(IEnumerable<FileInfo> source, string destination)
    {
        using var archive = ZipArchive.Create();

        foreach (var file in source) archive.AddEntry(file.Name, file.OpenRead());

        archive.SaveTo(destination, new ZipWriterOptions(CompressionType.LZMA){ArchiveComment = "Compressed by QuickCompress"});
    }

    public static void CompressDirectories(DirectoryInfo source, string destination)
        => CompressDirectories(new []{source}, destination);

    public static void CompressDirectories(IEnumerable<DirectoryInfo> source, string destination)
    {
        using var archive = ZipArchive.Create();

        var directoryInfos = source.ToList();
        foreach (var directory in directoryInfos)
        {
            foreach (var file in directory.GetFiles()) archive.AddEntry(Path.GetRelativePath(directory.FullName, file.FullName), file);
        }

        archive.SaveTo(destination, new ZipWriterOptions(CompressionType.LZMA){ArchiveComment = "Compressed by QuickCompress"});
    }

    public static void DecompressFiles(string source, string destination, string fileName, bool overwrite = true)
        => DecompressFiles(source, destination, new []{fileName}, overwrite);

    public static void DecompressFiles(string source, string destination, IEnumerable<string> fileNames, bool overwrite = true)
    {
        using var archive = ZipArchive.Open(source);

        foreach (var fileName in fileNames)
        {
            var entry = archive.Entries.First(x => x.Key == fileName && x.IsDirectory == false);
            entry.WriteToFile(destination, new ExtractionOptions {Overwrite = overwrite});
        }
    }

    public static void DecompressDirectories(string source, string destination, string directoryName, bool overwrite = false)
        => DecompressDirectories(source, destination, new []{directoryName}, overwrite);

    public static void DecompressDirectories(string source, string destination, IEnumerable<string> directoryNames, bool overwrite = false)
    {
        using var archive = ZipArchive.Open(source);
        foreach (var directoryName in directoryNames)
        {
            var entry = archive.Entries.First(x => x.Key == directoryName && x.IsDirectory);
            entry.WriteToDirectory(destination, new ExtractionOptions {Overwrite = overwrite});
        }
    }

    public static void DecompressAll(string source, string destination, bool overwrite = false)
    {
        using var archive = ZipArchive.Open(source);
        archive.WriteToDirectory(destination, new ExtractionOptions {Overwrite = overwrite});
    }


    public static void UpdateEntries(string source, string destination, string file)
        => UpdateEntries(source, destination, new []{file});

    public static void UpdateEntries(string source, string destination, IEnumerable<string> files)
    {
        using var archive = ZipArchive.Open(source);
        foreach (var file in files)
        {
            var entry = archive.Entries.First(x => x.Key == file);
            archive.RemoveEntry(entry);
            archive.AddEntry(entry.Key, file);
        }
    }

    public static void DeleteEntries(string source, string entry)
        => DeleteEntries(source, new []{entry});

    public static void DeleteEntries(string source, IEnumerable<string> entries)
    {
        using var archive = ZipArchive.Open(source);
        foreach (var file in entries)
        {
            var entry = archive.Entries.FirstOrDefault(x => x.Key == file);
            if (entry == null) continue;
            archive.RemoveEntry(entry);
        }
    }

    public static IEnumerable<string> ListFiles(this ZipArchive archive) => archive.Entries.Select(entry => entry.Key).ToList();
}
