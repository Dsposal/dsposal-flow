using Dsposal.Flow.Models.Cli;
using Dsposal.Flow.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DsposalForgeTests.Services;

public class ExcelFileServiceTests
{
    private static ExcelFileService CreateService() => new(NullLogger<ExcelFileService>.Instance);

    private static Options OptionsFor(params string[] paths) => new()
    {
        FilesAndDirs = paths,
        RecursiveDirSearch = true,
    };

    [Test]
    public void ResolveInputFiles_NoMatches_ReturnsFalse()
    {
        var missing = Path.Combine(Path.GetTempPath(), "dsposal-missing-" + Guid.NewGuid().ToString("N"));
        var opts = OptionsFor(missing);

        var result = CreateService().ResolveInputFiles(opts);

        Assert.That(result, Is.False);
        Assert.That(opts.ExcelFiles, Is.Empty);
    }

    [Test]
    public void ResolveInputFiles_ExistingXlsxFile_IsAdded()
    {
        var dir = NewTempDir();
        try
        {
            var file = Path.Combine(dir, "book.xlsx");
            File.WriteAllText(file, "x");

            var opts = OptionsFor(file);
            var result = CreateService().ResolveInputFiles(opts);

            Assert.That(result, Is.True);
            Assert.That(opts.ExcelFiles.Select(f => f.FullName), Is.EqualTo(new[] { file }));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void ResolveInputFiles_NonXlsxFile_IsIgnored()
    {
        var dir = NewTempDir();
        try
        {
            var file = Path.Combine(dir, "notes.txt");
            File.WriteAllText(file, "x");

            var opts = OptionsFor(file);

            Assert.That(CreateService().ResolveInputFiles(opts), Is.False);
            Assert.That(opts.ExcelFiles, Is.Empty);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void ResolveInputFiles_DirectoryNonRecursive_OnlyTopLevel()
    {
        var dir = NewTempDir();
        try
        {
            var top = Path.Combine(dir, "top.xlsx");
            File.WriteAllText(top, "x");
            var nestedDir = Path.Combine(dir, "nested");
            Directory.CreateDirectory(nestedDir);
            File.WriteAllText(Path.Combine(nestedDir, "deep.xlsx"), "x");

            var opts = OptionsFor(dir);
            opts.RecursiveDirSearch = false;

            var result = CreateService().ResolveInputFiles(opts);

            Assert.That(result, Is.True);
            Assert.That(opts.ExcelFiles.Select(f => f.FullName), Is.EqualTo(new[] { top }));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void ResolveInputFiles_DirectoryRecursive_FindsNested()
    {
        var dir = NewTempDir();
        try
        {
            var top = Path.Combine(dir, "top.xlsx");
            File.WriteAllText(top, "x");
            var nestedDir = Path.Combine(dir, "nested");
            Directory.CreateDirectory(nestedDir);
            var deep = Path.Combine(nestedDir, "deep.xlsx");
            File.WriteAllText(deep, "x");

            var opts = OptionsFor(dir);

            var result = CreateService().ResolveInputFiles(opts);

            Assert.That(result, Is.True);
            Assert.That(opts.ExcelFiles.Select(f => f.FullName), Is.EquivalentTo(new[] { top, deep }));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void ResolveInputFiles_DuplicatePaths_AreDeduplicated()
    {
        var dir = NewTempDir();
        try
        {
            var file = Path.Combine(dir, "book.xlsx");
            File.WriteAllText(file, "x");

            var opts = OptionsFor(file, file, dir); // same file directly and via the directory
            var result = CreateService().ResolveInputFiles(opts);

            Assert.That(result, Is.True);
            Assert.That(opts.ExcelFiles, Has.Count.EqualTo(1));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dsposal-excel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
