---
external help file: platyPS-help.xml
Module Name: platyPS
online version: https://github.com/PowerShell/platyPS/blob/master/docs/New-MarkdownHelpOption.md
schema: 2.0.0
---

# New-MarkdownHelpOption

## SYNOPSIS
Creates options to customize help workflow.

## SYNTAX

```
New-MarkdownHelpOption [[-Option] <MarkdownHelpOption>] [[-Path] <String>] [[-ReadMarkdown] <VisitMarkdown[]>]
 [[-WriteMarkdown] <VisitMarkdown[]>] [[-ReadCommand] <MamlCommandScriptHook[]>]
 [[-WriteCommand] <MamlCommandScriptHook[]>] [<CommonParameters>]
```

## DESCRIPTION
The **New-MarkdownHelpOption** cmdlet creates an options object that can be passed to markdown help cmdlets to customize PlatyPS behaviour.

## EXAMPLES

### Example 1
```powershell
PS C:\> $option = New-MarkdownHelpOption -WriteMarkdown {
  param($markdown, $path)

  # Return the new markdown
  return $markdown.Replace("`r`nPS C:\> ", "`r`n");
};

PS C:\> Update-MarkdownHelp .\docs\ -Option $option;
```

{{ Add example description here }}

## PARAMETERS

### -Option
An existing option object.

```yaml
Type: MarkdownHelpOption
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
{{Fill Path Description}}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReadCommand
One or more script blocks delegates to use when ReadCommand is called. For detailed examples see [about_platyPS_Extensibility].

```yaml
Type: MamlCommandScriptHook[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReadMarkdown
One or more script blocks delegates to use when ReadMarkdown is called. For detailed examples see [about_platyPS_Extensibility].

```yaml
Type: VisitMarkdown[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WriteCommand
One or more script blocks delegates to use when WriteCommand is called. For detailed examples see [about_platyPS_Extensibility].

```yaml
Type: MamlCommandScriptHook[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WriteMarkdown
One or more script blocks delegates to use when WriteMarkdown is called. For detailed examples see [about_platyPS_Extensibility].

```yaml
Type: VisitMarkdown[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### Markdown.MAML.Configuration.MarkdownHelpOption

## NOTES

## RELATED LINKS
