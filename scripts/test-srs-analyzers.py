"""Prove strict analyzers accept clean code and reject controlled defects."""

from pathlib import Path
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
parent = root / "tmp" / "package-G05" / "analyzer-controls"
parent.mkdir(parents=True, exist_ok=True)
probe = Path(tempfile.mkdtemp(prefix="probe-", dir=parent))
target = root / "scripts" / "srs-analyzers.targets"


def run(*args):
    result = subprocess.run(["dotnet", *map(str, args)], cwd=root, capture_output=True, text=True)
    return result.returncode, result.stdout + result.stderr


try:
    project = probe / "QualityProbe.csproj"
    project.write_text("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
""", encoding="utf-8")
    source = probe / "AnalyzerProbe.cs"
    source.write_text("""// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.QualityProbe;

/// <summary>Provides a clean analyzer control.</summary>
public static class AnalyzerProbe
{
    /// <summary>Returns the absolute input value.</summary>
    /// <param name="value">The input value.</param>
    /// <returns>The absolute value.</returns>
    public static int Absolute(int value) => System.Math.Abs(value);
}
""", encoding="utf-8")
    code, output = run("restore", project, "--configfile", root / "backend" / "NuGet.Config")
    assert code == 0, output
    common = ("build", project, "--no-restore", "-t:Rebuild", f"-p:CustomAfterMicrosoftCommonTargets={target}")
    code, output = run(*common)
    (probe / "clean.log").write_text(output, encoding="utf-8")
    assert code == 0, output
    source.write_text("""using System.Net.Mail;
using System.Text.RegularExpressions;

namespace CulinaryBlog.QualityProbe;

public static class AnalyzerProbe
{
    public static bool Matches(string input)
    {
        using var client = new SmtpClient("smtp.example.test") { EnableSsl = false };
        return Regex.IsMatch(input, "a+");
    }
}
""", encoding="utf-8")
    code, output = run(*common)
    (probe / "defects.log").write_text(output, encoding="utf-8")
    assert code != 0, "Controlled defects unexpectedly passed"
    for rule in ("S5332", "S6444", "SA1633", "SA1600"):
        assert f"error {rule}:" in output, f"{rule} did not reject the controlled defect:\n{output}"
    code, output = run(*common, f"-p:NuGetPackageRoot={probe / 'missing'}/")
    (probe / "missing-analyzers.log").write_text(output, encoding="utf-8")
    assert code != 0 and "Restore tools/QualityAnalyzers" in output, output
    print("PASS: clean code; strict S5332/S6444/SA1633/SA1600 rejection; missing DLL rejection")
    print(f"Control logs: {probe.relative_to(root)}")
finally:
    # Keep small evidence logs, remove only this run's verified build directories.
    assert probe.resolve().is_relative_to(parent.resolve())
    for name in ("bin", "obj"):
        build_dir = probe / name
        if build_dir.exists():
            shutil.rmtree(build_dir)
