---
external help file: platyPS-help.xml
Module Name: platyPS
online version: https://github.com/PowerShell/platyPS/blob/master/docs/Set-MarkdownHelpOption.md
schema: 2.0.0
---

# Set-MarkdownHelpOption

## SYNOPSIS

Save platyPS options.

## SYNTAX

```
Set-MarkdownHelpOption [[-Path] <String>] [-Option <MarkdownHelpOption>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION

Save platyPS options to disk.

## EXAMPLES

### Example 1

```powershell
PS C:\> Set-MarkdownHelpOption -Option @{ 'markdown.infostring' = 'text' };
```

Sets the infostring markdown option and saves the file as `.\.platyps.yml`.

## PARAMETERS

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Option

An existing object or a hashtable of options. For more information see [about_platyPS_Options].

```yaml
Type: MarkdownHelpOption
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Path

A path to a directory or individual YAML formatted file containing platyPS options. For more information see [about_platyPS_Options].

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### Markdown.MAML.Configuration.MarkdownHelpOption

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS
