Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path $PSScriptRoot\..\..).Path
$outFolder = Join-Path -Path $root -ChildPath out;
$testFolder = Join-Path -Path $outFolder -ChildPath tests/PlatyPS.Configuration;

# Clean path
Remove-Item -Path $testFolder -Force -Recurse -Confirm:$False -ErrorAction SilentlyContinue;
$Null = New-Item -Path $testFolder -ItemType Directory -Force;

Import-Module $outFolder/platyPS -Force
$MyIsLinux = Get-Variable -Name IsLinux -ValueOnly -ErrorAction SilentlyContinue
$MyIsMacOS = Get-Variable -Name IsMacOS -ValueOnly -ErrorAction SilentlyContinue
$global:IsUnix = $MyIsLinux -or $MyIsMacOS

# These test YAML configuration through .platyPS.yml
Describe 'PlatyPS configuration' -Tag 'configuration' {

    Context 'Use YAML configuration files' {

        It 'can get default options' {
            $options = New-MarkdownHelpOption -Option @{ };
            $options | Should -Not -BeNullOrEmpty;
        }

        $testPath = Join-Path -Path $testFolder -ChildPath MarkdownHelpOption;
        $yamlPath = Join-Path -Path $testPath -ChildPath .platyps.yaml;
        $ymlPath = Join-Path -Path $testPath -ChildPath .platyps.yml;

        It 'can write options to disk' {
            $option = New-MarkdownHelpOption -Option @{ 'markdown.infostring' = 'test-yaml' };
            $option | Set-MarkdownHelpOption -Path $yamlPath;

            Test-Path  -Path $yamlPath | Should -Be $True;

            $option = New-MarkdownHelpOption -Path $testPath;
            $option.Markdown.InfoString | Should Be 'test-yaml';
        }

        It 'can read .platyps.yaml' {
            $option = New-MarkdownHelpOption -Path $testPath;
            $option.Markdown.InfoString | Should Be 'test-yaml';
        }

        It 'can read .platyps.yml' {
            $option = New-MarkdownHelpOption -Option @{ 'markdown.infostring' = 'test-yml' };
            $option | Set-MarkdownHelpOption -Path $ymlPath;

            # .platyps.yml will take precidence over .yaml when both exist
            $option = New-MarkdownHelpOption -Path $testPath;
            $option.Markdown.InfoString | Should Be 'test-yml';
        }
    }
}
