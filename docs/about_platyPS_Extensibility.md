# platyPS_Extensibility.md

## about_platyPS_Extensibility.md

## SHORT DESCRIPTION

This document describes the options for extending PlatyPS.

## LONG DESCRIPTION

PlatyPS provides uses a pipeline model that can be extended using script delegates to implement custom functionality.
To this end PlatyPS provides four delegates `ReadMarkdown`, `WriteMarkdown`, `ReadCommand` and `WriteCommand` that can be implemented.
When implementing customisation, you can choose to implement any or all of the delegates.

The delegates are executed in the following order based on which cmdlet is being called:

- Update-MarkdownHelp
  - ReadMarkdown -> ReadCommand -> WriteCommand -> WriteMarkdown

- New-MarkdownHelp
  - WriteCommand -> WriteMarkdown

- New-ExternalHelp
  - ReadMarkdown -> ReadCommand

Each of the delegates can be implemented by using [New-MarkdownHelpOption] to create and option object that can be used with [Update-MarkdownHelp], [New-MarkdownHelp] or [New-ExternalHelp]

### ReadMarkdown

This delegate is called after markdown is read from disk and provides the markdown as a `string` to the delegate before parsing occurs. Consider implementing this delegate when:

- you want to include or dynamically generate content that is not included in markdown
- additional formatting or content is included in markdown for other systems, but should be removed before processing by PlatyPS

Example:

```powershell
# Remove DocFX Flavored Markdown(DFM) notes
$option = New-MarkdownHelpOption -ReadMarkdown {
  param($markdown, $path)

  # Return the new markdown
  return $markdown.Replace("`r`n> [!NOTE]`r`n", "`r`nNote:`r`n").Replace("`r`n> ", "`r`n");
};

Update-MarkdownHelp .\docs\ -Option $option;
```

### WriteMarkdown

The delegate is called before markdown is written to disk and provides the markdown as a `string` to the delegate after the cmdlet has been rendered as markdown. Consider implementing this delegate when:

- you want to rewrite markdown to meet style requirements
- include dynamic content based on when the cmdlet is updated

Example:

```powershell
# Remove PS C:\> prompt
$option = New-MarkdownHelpOption -WriteMarkdown {
  param($markdown, $path)

  # Return the new markdown
  return $markdown.Replace("`r`nPS C:\> ", "`r`n");
};

Update-MarkdownHelp .\docs\ -Option $option;
```

### ReadCommand

This delegate is called after markdown has been parsed and provides the delegate a `MamlCommand` model. This delegate is similar to `ReadMarkdown` except it provides access to a structured model instead of a markdown string.

Example:

```powershell

```

### WriteCommand

This delegate is called before the cmdlet is rendered as markdown and provides the delegate a `MamlCommand` model. This delegate is similar to `WriteMarkdown` except it provides access to a structured model instead of a markdown string.

Example:

```powershell
# Add line breaks between each line in notes
$option = New-MarkdownHelpOption -WriteCommand {
  param($command)

  $command.Notes.Text = $command.Notes.Text.Replace("`r`n", "`r`n`r`n");
};

Update-MarkdownHelp .\docs\ -Option $option;
```

## EXAMPLES

The following are examples of how to approach using PlatyPS extensibility to implement some of the most common feature requests.

### Removing PowerShell prefix from code examples

When using ` ```powershell` code blocks for web documentation the standard command-line prefix using in documentation (`PS>` or `PS C:\>`) won't be correct colorized.

```powershell
# Remove PowerShell prompts
$option = New-MarkdownHelpOption -WriteMarkdown {
  param($markdown, $path)

  # Return the new markdown
  return $markdown.Replace("`r`nPS C:\> ", "`r`n").Replace("`r`nPS> ", "`r`n");
};
```

## NOTE

Extensibility in PlatyPS is currently experimental, so please provide us feedback at https://github.com/PowerShell/PlatyPS/issues.

## SEE ALSO

{{ See also placeholder }}

{{ You can also list related articles, blogs, and video URLs. }}

## KEYWORDS

{{List alternate names or titles for this topic that readers might use.}}

- New-MarkdownHelpOption
- WriteMarkdown
- ReadMarkdown
- WriteCommand
- ReadCommand
