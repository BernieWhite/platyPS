using System;
using System.Linq;
using Markdown.MAML.Model;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Parser;
using Xunit;
using System.Collections.Generic;
using Markdown.MAML.Pipeline;

namespace Markdown.MAML.Test.Parser
{
    public sealed class ParserTests
    {
        const string headingText = "Heading Text";
        const string codeBlockText = "Code block text\r\non multiple lines";
        const string paragraphText = "Some text\r\non multiple\r\nlines";
        const string hyperlinkText = "Microsoft Corporation";
        const string hyperlinkUri = "https://go.microsoft.com/fwlink/?LinkID=135175&query=stuff";

        [Fact]
        public void ParsesHeadingsWithHashPrefix()
        {
            for (int i = 1; i <= 6; i++)
            {
                var token = ParseAndGetExpectedChild(MarkdownTokenType.Header, new string('#', i) + headingText + "\r\n");

                Assert.Equal(i, token.Depth);
                Assert.Equal(headingText, token.Text);
            }
        }

        [Fact]
        public void ParsesHeadingsWithUnderlines()
        {
            string[] headingUnderlines =
            {
                new String('=', headingText.Length),
                new String('-', headingText.Length)
            };

            for (int i = 1; i <= 2; i++)
            {
                var token = ParseAndGetExpectedChild(MarkdownTokenType.Header, headingText + "\r\n" + headingUnderlines[i - 1] + "\r\n");

                Assert.Equal(i, token.Depth);
                Assert.Equal(headingText, token.Text);
            }
        }

        [Fact]
        public void ParsesYamlHeader()
        {
            var markdown = string.Format("---\r\n{0}:{1}\r\n{2} : {3}\r\n---\r\n\r\n# {4}", "key1", "value1", "key2", "value2", "header");
            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal(3, tokens.Length);

            // Confirm key value pairs are extracted
            Assert.Equal(MarkdownTokenType.YamlKeyValue, tokens[0].Type);
            Assert.Equal("key1", tokens[0].Meta);
            Assert.Equal("value1", tokens[0].Text);
            Assert.Equal(MarkdownTokenType.YamlKeyValue, tokens[1].Type);
            Assert.Equal("key2", tokens[1].Meta);
            Assert.Equal("value2", tokens[1].Text);

            // Confirm that header follows
            Assert.Equal(MarkdownTokenType.Header, tokens[2].Type);
            Assert.Equal("header", tokens[2].Text);
        }

        [Fact]
        public void ParsesCodeBlock()
        {
            var markdown = string.Format("```\r\n{0}\r\n```\r\n", codeBlockText);
            var token = ParseAndGetExpectedChild(MarkdownTokenType.FencedBlock, markdown);

            Assert.Equal(codeBlockText, token.Text);
        }


        [Fact]
        public void ParsesCodeBlockWithLanguageSpecified()
        {
            var markdown = string.Format("```powershell\r\n{0}\r\n```\r\n", codeBlockText);
            var token = ParseAndGetExpectedChild(MarkdownTokenType.FencedBlock, markdown);

            Assert.Equal(codeBlockText, token.Text);
            Assert.Equal("powershell", token.Meta);
        }

        [Fact]
        public void ParsesHyperlink()
        {
            var markdown = string.Format("[{0}]({1})", hyperlinkText, hyperlinkUri);
            var token = ParseAndGetExpectedChild(MarkdownTokenType.Link, markdown);

            Assert.Equal(hyperlinkText, token.Meta);
            Assert.Equal(hyperlinkUri, token.Text);
        }

        [Fact]
        public void ParsesHyperlinkWithoutLink()
        {
            var markdown = string.Format("[{0}]()", hyperlinkText);
            var token = ParseAndGetExpectedChild(MarkdownTokenType.Link, markdown);

            Assert.Equal(hyperlinkText, token.Meta);
            Assert.Equal(string.Empty, token.Text);
        }

        [Fact]
        public void ParsesLinkReference()
        {
            //
            var markdown = @"
[long-reference][xref:long-reference]
[shortcut-reference]

\[Not-a-link\] [] also not a link

[xref:long-reference]: long.md
[shortcut-reference]: shortcut.md
";
            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal(5, tokens.Count());
            Assert.Equal(MarkdownTokenType.LinkReference, tokens[0].Type);
            Assert.Equal("long-reference", tokens[0].Meta);
            Assert.Equal("xref:long-reference", tokens[0].Text);

            Assert.Equal(MarkdownTokenType.LinkReference, tokens[1].Type);
            Assert.Equal("shortcut-reference", tokens[1].Meta);
            Assert.Equal("shortcut-reference", tokens[1].Text);

            Assert.Equal(MarkdownTokenType.Text, tokens[2].Type);
            Assert.Equal("[Not-a-link] [] also not a link", tokens[2].Text);

            Assert.Equal(MarkdownTokenType.LinkReferenceDefinition, tokens[3].Type);
            Assert.Equal("xref:long-reference", tokens[3].Meta);
            Assert.Equal("long.md", tokens[3].Text);

            Assert.Equal(MarkdownTokenType.LinkReferenceDefinition, tokens[4].Type);
            Assert.Equal("shortcut-reference", tokens[4].Meta);
            Assert.Equal("shortcut.md", tokens[4].Text);
        }

        [Fact]
        public void TextSpansCanContainDoubleQuotes()
        {
            var markdown = @"
# Foo
This is a :""text"" with doublequotes
";
            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal(2, tokens.Count());
            Assert.Equal(MarkdownTokenType.Text, tokens[1].Type);
            Assert.Equal(@"This is a :""text"" with doublequotes", tokens[1].Text);
        }

        [Fact]
        public void ListInlineCharactersDontAffectLineEndings()
        {
            var markdown = @"
This is text * with a asterix.
This is text - with dash.
";
            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal(2, tokens.Length);
            Assert.Equal(MarkdownTokenType.Text, tokens[0].Type);
            Assert.Equal(MarkdownTokenFlag.LineEnding, tokens[0].Flag);
            Assert.Equal("This is text * with a asterix.", tokens[0].Text);
            Assert.Equal(MarkdownTokenType.Text, tokens[1].Type);
            Assert.Equal(MarkdownTokenFlag.LineEnding, tokens[1].Flag);
            Assert.Equal("This is text - with dash.", tokens[1].Text);
        }

        [Fact]
        public void TextSpansCanContainBrackets()
        {
            var markdown = @"
# Foo
about_Hash_Tables (http://go.microsoft.com/fwlink/?LinkID=135175).
";
            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal(2, tokens.Count());
            Assert.Equal(MarkdownTokenType.Text, tokens[1].Type);
            Assert.Equal(@"about_Hash_Tables (http://go.microsoft.com/fwlink/?LinkID=135175).", tokens[1].Text);
        }

        [Fact]
        public void ParsesParagraphWithSupportedCharacters()
        {
            var markdown = "This is a \"test\" string; it's very helpful.  Success: yes!?";

            var token = ParseAndGetExpectedChild(MarkdownTokenType.Text, markdown);

            Assert.Equal(markdown, token.Text);
        }

        [Fact]
        public void ParsesEscapedLessAndMoreCorrectly()
        {
            var markdown = @"\<port-number\>";

            var token = ParseAndGetExpectedChild(MarkdownTokenType.Text, markdown);

            Assert.Equal("<port-number>", token.Text);
        }

        [Fact]
        public void ParsesParagraphWithFormattedSpans()
        {
            var markdown = "Normal\r\n\r\nText *Italic*  \r\n\r\n**Bold**\r\n _Italic2_\r\n __Bold2__\r\n### New header!\r\nBoooo\r\n----\r\n";

            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal("Normal", tokens[0].Text);
            Assert.Equal("Text", tokens[1].Text.Trim());
            Assert.Equal("Italic", tokens[2].Text);
            Assert.Equal("Bold", tokens[3].Text);
            Assert.Equal("Italic2", tokens[4].Text);
            Assert.Equal("Bold2", tokens[5].Text);
        }

        [Fact]
        public void ParsesDocumentWithMultipleNodes()
        {
            string documentText = string.Format(@"
# {0}

{2}

```
{1}
```

## {0}
{2} [{3}]({4})
", headingText, codeBlockText, paragraphText, hyperlinkText, hyperlinkUri);

            var tokens = MarkdownStringToTokens(documentText);

            Assert.Equal(MarkdownTokenType.Header, tokens[0].Type);
            Assert.Equal(headingText, tokens[0].Text);
            Assert.Equal(1, tokens[0].Depth);

            Assert.Equal(MarkdownTokenType.Text, tokens[1].Type);
            Assert.Equal(paragraphText.Replace("\r\n", " "), string.Join(" ", tokens[1].Text, tokens[2].Text, tokens[3].Text));

            Assert.Equal(MarkdownTokenType.FencedBlock, tokens[4].Type);
            Assert.Equal(codeBlockText, tokens[4].Text);

            Assert.Equal(MarkdownTokenType.Header, tokens[5].Type);
            Assert.Equal(headingText, tokens[5].Text);
            Assert.Equal(2, tokens[5].Depth);

            Assert.Equal(MarkdownTokenType.Text, tokens[6].Type);

            Assert.Equal(MarkdownTokenType.Link, tokens[9].Type);
            Assert.Equal(hyperlinkText, tokens[9].Meta);
            Assert.Equal(hyperlinkUri, tokens[9].Text);
        }

        [Fact]
        public void CanPaserEmptySourceBlock()
        {
            var tokens = MarkdownStringToTokens(
@"#### 1:

```powershell
```

```powershell
[Parameter(
  ValueFromPipeline = $true,
  ParameterSetName = 'Set 1')]
```
");

            Assert.Equal(3, tokens.Count());

            Assert.Equal(MarkdownTokenType.Header, tokens[0].Type);
            Assert.Equal(4, tokens[0].Depth);

            Assert.Equal(MarkdownTokenType.FencedBlock, tokens[1].Type);
            Assert.Equal("powershell", tokens[1].Meta);
            Assert.Equal(string.Empty, tokens[1].Text);

            Assert.Equal(MarkdownTokenType.FencedBlock, tokens[2].Type);
            Assert.Equal("powershell", tokens[2].Meta);
            Assert.Equal(@"[Parameter(
  ValueFromPipeline = $true,
  ParameterSetName = 'Set 1')]", tokens[2].Text);
        }

        [Fact]
        public void PreserveLineEndingsInLists()
        {
            var actual = MarkdownStringToString(
@"
-- This is a list
-- Yes, with double-dashes
-- Because that's how it happens a lot in PS docs

- This is a regular list
- Item2
- Item3

* Item1
* Item1
* Item3

And this is not a list.

New paragraph
");

            Common.AssertMultilineEqual(
@"-- This is a list
-- Yes, with double-dashes
-- Because that's how it happens a lot in PS docs

- This is a regular list
- Item2
- Item3

* Item1
* Item1
* Item3

And this is not a list.

New paragraph
", actual);
        }

        [Fact]
        public void PreserveLineEndingsInLists2()
        {
            var tokens = MarkdownStringToTokens(
@"
Valid values are:

-- Block: When the output buffer is full, execution is suspended until the buffer is clear. 
-- Drop: When the output buffer is full, execution continues. As new output is saved, the oldest output is discarded.
-- None: No output buffering mode is specified. The value of the OutputBufferingMode property of the session configuration is used for the disconnected session.");

            Assert.Equal(4, tokens.Length);
            Assert.Equal(MarkdownTokenType.Text, tokens[0].Type);
            Assert.Equal("Valid values are:", tokens[0].Text);
            Assert.Equal(MarkdownTokenFlag.Preserve | MarkdownTokenFlag.LineBreak, tokens[0].Flag);

            Assert.Equal(MarkdownTokenType.Text, tokens[1].Type);
            Assert.Equal("-- Block: When the output buffer is full, execution is suspended until the buffer is clear. ", tokens[1].Text);
            Assert.Equal(MarkdownTokenFlag.PreserveLineEnding, tokens[1].Flag);

            Assert.Equal(MarkdownTokenType.Text, tokens[2].Type);
            Assert.Equal("-- Drop: When the output buffer is full, execution continues. As new output is saved, the oldest output is discarded.", tokens[2].Text);
            Assert.Equal(MarkdownTokenFlag.PreserveLineEnding, tokens[2].Flag);

            Assert.Equal(MarkdownTokenType.Text, tokens[3].Type);
            Assert.Equal("-- None: No output buffering mode is specified. The value of the OutputBufferingMode property of the session configuration is used for the disconnected session.", tokens[3].Text);
            Assert.Equal(MarkdownTokenFlag.None, tokens[3].Flag);

//            Common.AssertMultilineEqual(@"Valid values are:
//-- Block: When the output buffer is full, execution is suspended until the buffer is clear. 
//-- Drop: When the output buffer is full, execution continues. As new output is saved, the oldest output is discarded.
//-- None: No output buffering mode is specified. The value of the OutputBufferingMode property of the session configuration is used for the disconnected session.", tokens[1].Text);
        }

        [Fact]
        public void ParsesCrossPlatformLineBreaks()
        {
            var markdown = "# Get-LineBreak\r\n\n## -Description\r\n\r\nThis is a list containing multiple line breaks:\r\n\r\n- Item 1\n- Item 2\r\n\r\n";

            var tokens = MarkdownStringToTokens(markdown);

            Assert.Equal(5, tokens.Count());

            Assert.Equal(MarkdownTokenType.Header, tokens[0].Type);
            Assert.True(tokens[0].IsDoubleLineEnding());
            Assert.Equal(MarkdownTokenType.Text, tokens[3].Type);
            Assert.True(tokens[3].IsSingleLineEnding());
            Assert.Equal(MarkdownTokenType.Text, tokens[4].Type);
            Assert.True(tokens[4].IsDoubleLineEnding());
        }

        [Fact]
        public void PreserveTextAsIsInFormattingPreserveMode()
        {
            string expected = @"Hello:


            -- Block: aaa. Foo
            Bar [this](hyperlink)

            -- Drop: <When the> output buffer is full
            * None: specified.
            It's up
               To authors
                  To format text
            ";

            var actual = MarkdownStringToString(expected, preserveFormatting: true);

            Common.AssertMultilineEqual(expected, actual);
        }

        [Fact]
        public void UnderstandsOneLineBreakVsTwoLineBreaks()
        {
            var tokens = MarkdownStringToTokens(@"
1
2

3
");

            Assert.Equal(3, tokens.Length);
            Assert.Equal(MarkdownTokenType.Text, tokens[0].Type);
            Assert.Equal("1", tokens[0].Text);
            Assert.True(tokens[0].IsSingleLineEnding());
            Assert.Equal(MarkdownTokenType.Text, tokens[1].Type);
            Assert.Equal("2", tokens[1].Text);
            Assert.True(tokens[1].IsDoubleLineEnding());
            Assert.Equal(MarkdownTokenType.Text, tokens[2].Type);
            Assert.Equal("3", tokens[2].Text);
        }

        [Fact]
        public void ParseEscapingSameWayAsGithub()
        {
            var tokens = MarkdownStringToTokens(@"
\<
\\<
\\\<
\\\\<
\\\\\<
\\\\[
\
\\
\\\
\\\\
(
)
[
]
\(
\)
\[
\\[
\]
\`
");

            //Assert.Equal(3, tokens.Length);
            Assert.Equal(MarkdownTokenType.Text, tokens[0].Type);

            // NOTE: to update this example, create a gist on github to check out how it's parsed.
            Assert.Equal(@"< \< \< \\< \\< \\[ \ \ \\ \\ ( ) [ ] ( ) [ \[ ] `", string.Join(" ", tokens.Select(t => t.Text)));
        }

        [Fact]
        public void GetYamlMetadataWorks()
        {
            var map = PipelineBuilder.ToMetadata().Process(@"---
foo: foo1

bar: bar1
---

foo: bar # this is not part of yaml metadata
");

            Assert.Equal("foo1", map["foo"]);
            Assert.Equal("bar1", map["bar"]);
            Assert.Equal(2, map.Count);
        }

        [Fact]
        public void ParsesExample3FromGetPSSnapin()
        {
            string codeblockText = 
@"The first command gets snap-ins that have been added to the current session, including the snap-ins that are installed with Windows PowerShell. In this example, ManagementFeatures is not returned. This indicates that it has not been added to the session.
PS C:\>get-pssnapin

The second command gets snap-ins that have been registered on your system (including those that have already been added to the session). It does not include the snap-ins that are installed with Windows PowerShell.In this case, the command does not return any snap-ins. This indicates that the ManagementFeatures snapin has not been registered on the system.
PS C:\>get-pssnapin -registered

The third command creates an alias, ""installutil"", for the path to the InstallUtil tool in .NET Framework.
PS C:\>set-alias installutil $env:windir\Microsoft.NET\Framework\v2.0.50727\installutil.exe

The fourth command uses the InstallUtil tool to register the snap-in. The command specifies the path to ManagementCmdlets.dll, the file name or ""module name"" of the snap-in.
PS C:\>installutil C:\Dev\Management\ManagementCmdlets.dll

The fifth command is the same as the second command. This time, you use it to verify that the ManagementCmdlets snap-in is registered.
PS C:\>get-pssnapin -registered

The sixth command uses the Add-PSSnapin cmdlet to add the ManagementFeatures snap-in to the session. It specifies the name of the snap-in, ManagementFeatures, not the file name.
PS C:\>add-pssnapin ManagementFeatures

To verify that the snap-in is added to the session, the seventh command uses the Module parameter of the Get-Command cmdlet. It displays the items that were added to the session by a snap-in or module.
PS C:\>get-command -module ManagementFeatures

You can also use the PSSnapin property of the object that the Get-Command cmdlet returns to find the snap-in or module in which a cmdlet originated. The eighth command uses dot notation to find the value of the PSSnapin property of the Set-Alias cmdlet.
PS C:\>(get-command set-alias).pssnapin";
            string descriptionText =
                @"This example demonstrates the process of registering a snap-in on your system and then adding it to your session. It uses ManagementFeatures, a fictitious snap-in implemented in a file called ManagementCmdlets.dll.";
            string documentText = string.Format(@"
#### -------------------------- EXAMPLE 3 --------------------------

```powershell
{0}

```
{1}


### RELATED LINKS
[Online Version:](http://go.microsoft.com/fwlink/p/?linkid=289570)
[Get-PSSnapin]()
[Remove-PSSnapin]()
[about_Profiles]()
[about_PSSnapins]()

## Clear-History

### SYNOPSIS
Deletes entries from the command history.

### DESCRIPTION
The Clear-History cmdlet deletes commands from the command history, that is, the list of commands entered during the current session.
Without parameters, Clear-History deletes all commands from the session history, but you can use the parameters of Clear-History to delete selected commands.

### PARAMETERS

#### CommandLine [String[]]

```powershell
[Parameter(ParameterSetName = 'Set 2')]
```

Deletes commands with the specified text strings. If you enter more than one string, Clear-History deletes commands with any of the strings.

", codeblockText, descriptionText);

            var tokens = MarkdownStringToTokens(documentText);

            Assert.Equal(MarkdownTokenType.Header, tokens[0].Type);
            Assert.Equal(4, tokens[0].Depth);

            Assert.Equal(MarkdownTokenType.FencedBlock, tokens[1].Type);
            Common.AssertMultilineEqual(codeblockText, tokens[1].Text);

            Assert.Equal(MarkdownTokenType.Text, tokens[2].Type);
            Common.AssertMultilineEqual(descriptionText, tokens[2].Text);
        }

        [Fact]
        public void PreservesLineBreakAfterHeaderWithHashPrefix()
        {
            var expectedSynopsis = "This is the synopsis text.";
            var expectedDescription = "This is the description text.";

            // Parse markdown
            var tokens = MarkdownStringToTokenStream($"## SYNOPSIS\r\n{expectedSynopsis}\r\n\r\n## DESCRIPTION\r\n\r\n{expectedDescription}");

            // Get results
            var actualSynopsis = GetParagraph(tokens, "SYNOPSIS").Text;
            var actualDescription = GetParagraph(tokens, "DESCRIPTION").Text;
            var synopsisHasLineBreak = GetHeader(tokens, "SYNOPSIS").IsDoubleLineEnding();
            var descriptionHasLineBreak = GetHeader(tokens, "DESCRIPTION").IsDoubleLineEnding();

            // Check that text matches and line breaks haven't been captured as text
            Assert.Equal(expectedSynopsis, actualSynopsis);
            Assert.Equal(expectedDescription, actualDescription);

            // Does not use line break and should not be added
            Assert.False(synopsisHasLineBreak);

            // Uses line break and should be preserved
            Assert.True(descriptionHasLineBreak);
        }

        [Fact]
        public void PreservesLineBreakAfterParameter()
        {
            var expectedP1 = "Name parameter description.";
            var expectedP2 = "Path parameter description.";

            // Parse markdown
            var tokens = MarkdownStringToTokenStream($"## PARAMETERS\r\n\r\n### -Name\r\n{expectedP1}\r\n\r\n```yaml\r\n```\r\n\r\n### -Path\r\n\r\n{expectedP2}");

            var actualP1 = GetParagraph(tokens, "-Name").Text;
            var actualP2 = GetParagraph(tokens, "-Path").Text;
            var hasLineBreakP1 = GetHeader(tokens, "-Name").IsDoubleLineEnding();
            var hasLineBreakP2 = GetHeader(tokens, "-Path").IsDoubleLineEnding();

            // Check that text matches and line breaks haven't been captured as text
            Assert.Equal(expectedP1, actualP1);
            Assert.Equal(expectedP2, actualP2);

            // Does not use line break and should not be added
            Assert.False(hasLineBreakP1);

            // Uses line break and should be preserved
            Assert.True(hasLineBreakP2);
        }

        [Fact]
        public void PreservesLineBreakAfterHeaderWithUnderlines()
        {
            var expectedSynopsis = "This is the synopsis text.";
            var expectedDescription = "This is the description text.";

            // Parse markdown
            var tokens = MarkdownStringToTokenStream($"SYNOPSIS\r\n---\r\n{expectedSynopsis}\r\n\r\nDESCRIPTION\r\n---\r\n\r\n{expectedDescription}");

            // Get results
            var actualSynopsis = GetParagraph(tokens, "SYNOPSIS").Text;
            var actualDescription = GetParagraph(tokens, "DESCRIPTION").Text;
            var synopsisHasLineBreak = GetHeader(tokens, "SYNOPSIS").IsDoubleLineEnding();
            var descriptionHasLineBreak = GetHeader(tokens, "DESCRIPTION").IsDoubleLineEnding();

            // Check that text matches and line breaks haven't been captured as text
            Assert.Equal(expectedSynopsis, actualSynopsis);
            Assert.Equal(expectedDescription, actualDescription);

            // Does not use line break and should not be added
            Assert.False(synopsisHasLineBreak);

            // Uses line break and should be preserved
            Assert.True(descriptionHasLineBreak);
        }

        private MarkdownToken ParseAndGetExpectedChild(MarkdownTokenType tokenType, string markdownString)
        {
            var tokens = MarkdownStringToTokens(markdownString);
            return AssertTokenType(tokenType, tokens[0]);
        }

        private MarkdownToken AssertTokenType(MarkdownTokenType tokenType, MarkdownToken token)
        {
            Assert.NotNull(token);
            Assert.Equal(tokenType, token.Type);
            return token;
        }

        private MarkdownToken GetParagraph(TokenStream stream, string heading)
        {
            return stream.GetSection(heading).FirstOrDefault(token => token.Type == MarkdownTokenType.Text);
        }

        private MarkdownToken GetHeader(TokenStream stream, string text = null)
        {
            return stream
                // If heading was specified, get the specific heading
                .FirstOrDefault(token => token.Type == MarkdownTokenType.Header && (string.IsNullOrEmpty(text) || token.Text == text));
        }

        private MarkdownToken[] MarkdownStringToTokens(string markdown)
        {
            var reader = new MarkdownReader(preserveFormatting: false, yamlHeaderOnly: false);
            return reader.Read(markdown, null).ToArray();
        }

        private string MarkdownStringToString(string markdown, bool preserveFormatting = false)
        {
            var reader = new MarkdownReader(preserveFormatting, yamlHeaderOnly: false);
            var tokens = reader.Read(markdown, null);

            var writer = new MarkdownWriter(tokens);
            return writer.Write();
        }

        private TokenStream MarkdownStringToTokenStream(string markdown)
        {
            var reader = new MarkdownReader(preserveFormatting: false, yamlHeaderOnly: false);
            return reader.Read(markdown, null);
        }
    }
}
