Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path $PSScriptRoot\..\..).Path
$outFolder = Join-Path -Path $root -ChildPath out;
$testFolder = Join-Path -Path $outFolder -ChildPath tests/PlatyPS.Extensibility;

# Clean path
Remove-Item -Path $testFolder -Force -Recurse -Confirm:$False -ErrorAction SilentlyContinue;
$Null = New-Item -Path $testFolder -ItemType Directory -Force;

Import-Module $outFolder/platyPS -Force
$MyIsLinux = Get-Variable -Name IsLinux -ValueOnly -ErrorAction SilentlyContinue
$MyIsMacOS = Get-Variable -Name IsMacOS -ValueOnly -ErrorAction SilentlyContinue
$global:IsUnix = $MyIsLinux -or $MyIsMacOS


Describe 'PlatyPS pipeline' -Tag 'pipeline' {

    Context 'ReadMarkdown' {

        function global:Test-ReadMarkdownHook {}

        $targetDoc = Join-Path -Path $testFolder -ChildPath 'Test-ReadMarkdownHook.md';
        New-MarkdownHelp -OutputFolder $testFolder -Force -Command 'Test-ReadMarkdownHook' -WarningAction SilentlyContinue;

        It 'is called during Update-MarkdownHelp' {

            # Change the description
            $option = New-MarkdownHelpOption -ReadMarkdown {
                param($markdown, $path)

                return $markdown.Replace("{{Fill in the Description}}", "This is the description.");
            };

            Update-MarkdownHelp -Path $targetDoc -Option $option -WarningAction SilentlyContinue;

            $targetDoc | Should -FileContentMatchMultiline 'This is the description.'
        }

        It 'is called during New-ExternalHelp' {

            # Change the description
            $option = New-MarkdownHelpOption -ReadMarkdown {
                param($markdown, $path)

                return $markdown.Replace("This is the description", "This is a new description.");
            };

            New-ExternalHelp -Path $targetDoc -Option $option -OutputPath (Join-Path -Path $testFolder -ChildPath 'platyPS-ReadMarkdown.xml') -Force;

            (Join-Path -Path $testFolder -ChildPath 'platyPS-ReadMarkdown.xml') | Should -FileContentMatchMultiline 'This is a new description.'
        }
    }

    Context 'WriteMarkdown' {

        function global:Test-WriteMarkdownHook {}

        $targetDoc = Join-Path -Path $testFolder -ChildPath 'Test-WriteMarkdownHook.md';

        It 'is called during New-MarkdownHelp' {

            # Change the description
            $option = New-MarkdownHelpOption -WriteMarkdown {
                param($markdown, $path)

                return $markdown.Replace("{{Fill in the Description}}", "This is the description.");
            };

            New-MarkdownHelp -OutputFolder $testFolder -Option $option -Force -Command 'Test-WriteMarkdownHook' -WarningAction SilentlyContinue;

            $targetDoc | Should -FileContentMatchMultiline 'This is the description.'
        }

        It 'is called during Update-MarkdownHelp' {

            # Remove Windows PS prompt
            $option = New-MarkdownHelpOption -WriteMarkdown {
                param($markdown, $path)

                return $markdown.Replace("`r`nPS C:\> ", "`r`n");
            };

            Update-MarkdownHelp -Path $targetDoc -Option $option -WarningAction SilentlyContinue;

            $targetDoc | Should -Not -FileContentMatchMultiline 'PS C:\\\>'
        }
    }

    Context 'ReadCommand' {

        function global:Test-ReadCommandHook {}

        $targetDoc = Join-Path -Path $testFolder -ChildPath 'Test-ReadCommandHook.md';
        New-MarkdownHelp -OutputFolder $testFolder -Force -Command 'Test-ReadCommandHook' -WarningAction SilentlyContinue;

        It 'is called during Update-MarkdownHelp' {

            # Change the description
            $option = New-MarkdownHelpOption -ReadCommand {
                param($command)
                
                $command.Description.Text = 'This is the description.';
            };

            Update-MarkdownHelp -Path $targetDoc -Option $option -WarningAction SilentlyContinue;

            $targetDoc | Should -FileContentMatchMultiline 'This is the description.'
        }

        It 'is called during New-ExternalHelp' {

            # Change the description
            $option = New-MarkdownHelpOption -ReadCommand {
                param($command)

               $command.Description.Text = 'This is a new description.';
            };

            New-ExternalHelp -Path $targetDoc -Option $option -OutputPath (Join-Path -Path $testFolder -ChildPath 'platyPS-ReadCommand.xml') -Force;

            (Join-Path -Path $testFolder -ChildPath 'platyPS-ReadCommand.xml') | Should -FileContentMatchMultiline 'This is a new description.'
        }
    }

    Context 'WriteCommand' {

        function global:Test-WriteCommandHook {
            param (
                [Switch]$AsJob
            )
        }

        $targetDoc = Join-Path -Path $testFolder -ChildPath 'Test-WriteCommandHook.md';

        # Remove AsJob parameter by name
        $option = New-MarkdownHelpOption -WriteCommand {
            param($command)

            $command.RemoveParameter('AsJob');
        };

        It 'is called during New-MarkdownHelp' {

            # TODO: Issue, parameters are removed but syntax is not updated

            New-MarkdownHelp -OutputFolder $testFolder -Option $option -Force -Command 'Test-WriteCommandHook' -WarningAction SilentlyContinue;

            $targetDoc | Should -Not -FileContentMatchMultiline 'AsJob'
        }

        It 'is called during Update-MarkdownHelp' {

            Update-MarkdownHelp -Path $targetDoc -Option $option -WarningAction SilentlyContinue;

            $targetDoc | Should -Not -FileContentMatchMultiline 'AsJob'
        }
    }
}
