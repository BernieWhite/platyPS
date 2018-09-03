#region PlatyPS

## DEVELOPERS NOTES & CONVENTIONS
##
##  1. Non-exported functions (subroutines) should avoid using
##     PowerShell standard Verb-Noun naming convention.
##     They should use camalCase or PascalCase instead.
##  2. SMALL subroutines, used only from ONE function
##     should be placed inside the parent function body.
##     They should use camalCase for the name.
##  3. LARGE subroutines and subroutines used from MORE THEN ONE function
##     should be placed after the IMPLEMENTATION text block in the middle
##     of this module.
##     They should use PascalCase for the name.
##  4. Add comment "# yeild" on subroutine calls that write values to pipeline.
##     It would help keep code maintainable and simplify ramp up for others.
##

## Script constants

$script:EXTERNAL_HELP_FILE_YAML_HEADER = 'external help file'
$script:ONLINE_VERSION_YAML_HEADER = 'online version'
$script:SCHEMA_VERSION_YAML_HEADER = 'schema'
$script:APPLICABLE_YAML_HEADER = 'applicable'

$script:UTF8_NO_BOM = New-Object System.Text.UTF8Encoding -ArgumentList $False
$script:SET_NAME_PLACEHOLDER = 'UNNAMED_PARAMETER_SET'
# TODO: this is just a place-holder, we can do better
$script:DEFAULT_MAML_XML_OUTPUT_NAME = 'rename-me-help.xml'

$script:MODULE_PAGE_MODULE_NAME = "Module Name"
$script:MODULE_PAGE_GUID = "Module Guid"
$script:MODULE_PAGE_LOCALE = "Locale"
$script:MODULE_PAGE_FW_LINK = "Download Help Link"
$script:MODULE_PAGE_HELP_VERSION = "Help Version"
$script:MODULE_PAGE_ADDITIONAL_LOCALE = "Additional Locale"

$script:MAML_ONLINE_LINK_DEFAULT_MONIKER = 'Online Version:'

[Markdown.MAML.Configuration.MarkdownHelpOption]::GetWorkingPath = {

    $Null = validateWorkingProvider;

    return Get-Location;
}

function New-MarkdownHelp
{
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo[]])]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true, ParameterSetName="FromModule")]
        [string[]]$Module,

        [Parameter(Mandatory=$true, ParameterSetName="FromCommand")]
        [string[]]$Command,

        [Parameter(Mandatory=$true, ParameterSetName="FromMaml")]
        [string[]]$MamlFile,

        [Parameter(ParameterSetName="FromModule")]
        [Parameter(ParameterSetName="FromCommand")]
        [System.Management.Automation.Runspaces.PSSession]$Session,

        [Parameter(ParameterSetName="FromMaml")]
        [switch]$ConvertNotesToList,

        [Parameter(ParameterSetName="FromMaml")]
        [switch]$ConvertDoubleDashLists,

        [switch]$Force,

        [switch]$AlphabeticParamsOrder,

        [hashtable]$Metadata,

        [Parameter(ParameterSetName="FromCommand")]
        [string]$OnlineVersionUrl = '',

        [Parameter(Mandatory=$true)]
        [string]$OutputFolder,

        [switch]$NoMetadata,

        [switch]$UseFullTypeName,

        [System.Text.Encoding]$Encoding = $script:UTF8_NO_BOM,

        [Parameter(ParameterSetName="FromModule")]
        [Parameter(ParameterSetName="FromMaml")]
        [switch]$WithModulePage,

        [Parameter(ParameterSetName="FromModule")]
        [Parameter(ParameterSetName="FromMaml")]
        [string]$Locale = "en-US",

        [Parameter(ParameterSetName="FromModule")]
        [Parameter(ParameterSetName="FromMaml")]
        [string]$HelpVersion = "{{Please enter version of help manually (X.X.X.X) format}}",

        [Parameter(ParameterSetName="FromModule")]
        [Parameter(ParameterSetName="FromMaml")]
        [string]$FwLink = "{{Please enter FwLink manually}}",

        [Parameter(ParameterSetName="FromMaml")]
        [string]$ModuleName = "MamlModule",

        [Parameter(ParameterSetName="FromMaml")]
        [string]$ModuleGuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",

        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.MarkdownHelpOption]$Option
    )

    begin {
        $perfTrace = New-Object -TypeName System.Diagnostics.Stopwatch;
        $perfTrace.Start();

        validateWorkingProvider
        $Null = New-Item -Type Directory $OutputFolder -ErrorAction SilentlyContinue;

        $Option = New-MarkdownHelpOption -Option $Option;

        # Build a ToMarkdown pipeline
        $pipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMarkdown($Option).Configure({
            param ($config)

            $config.UseFirstExample();

            if ($NoMetadata) {
                $config.UseNoMetadata();
            }

            if ($AlphabeticParamsOrder) {
                $config.UseSortParamsAlphabetic();
            }

            $config.SetOnlineVersionUrl();
        }).Build();
    }

    process {

        function processMamlObjectToFile {
            param(
                [Parameter(ValueFromPipeline=$true)]
                [ValidateNotNullOrEmpty()]
                [Markdown.MAML.Model.MAML.MamlCommand]$mamlObject
            )

            process {

                $commandName = $mamlObject.Name

                # create markdown
                if (-Not $NoMetadata) {
                    # get help file name
                    if ($MamlFile) {
                        $helpFileName = Split-Path -Leaf -Path $MamlFile
                    }
                    else {
                        $a = @{
                            Name = $commandName
                        }

                        if ($module) {
                            # for module case, scope it just to this module
                            $a['Module'] = $module
                        }

                        $helpFileName = GetHelpFileName (Get-Command @a)
                    }

                    Write-Verbose "Maml things module is: $($mamlObject.ModuleName)"
                    $mamlObject.OnlineVersionUrl = $OnlineVersionUrl;

                    $newMetadata = ($Metadata + @{
                        $script:EXTERNAL_HELP_FILE_YAML_HEADER = $helpFileName
                    })
                }

                if ($Null -ne $newMetadata)
                {
                    $mamlObject.SetMetadata($newMetadata);
                }
        
                $md = $pipeline.Process($mamlObject);
                
                MySetContent -path (Join-Path $OutputFolder "$commandName.md") -value $md -Encoding $Encoding -Force:$Force
            }
        }

        if ($NoMetadata -and $Metadata) {
            throw '-NoMetadata and -Metadata cannot be specified at the same time'
        }

        if ($PSCmdlet.ParameterSetName -eq 'FromCommand') {
            $command | ForEach-Object {
                if (-not (Get-Command $_ -EA SilentlyContinue))
                {
                    throw "Command $_ not found in the session."
                }

                GetMamlObject -Session $Session -Cmdlet $_ -UseFullTypeName:$UseFullTypeName | processMamlObjectToFile
            }
        }
        else {
            if ($module) {
                $iterator = $module
            }
            else {
                $iterator = $MamlFile
            }

            $iterator | ForEach-Object {
                if ($PSCmdlet.ParameterSetName -eq 'FromModule') {
                    $moduleName = $_;

                    if (-not (HasModule -Module $moduleName)) {
                        throw "Module $moduleName is not imported in the session. Run 'Import-Module $moduleName'."
                    }

                    GetMamlObject -Session $Session -Module $moduleName -UseFullTypeName:$UseFullTypeName | processMamlObjectToFile

                    $ModuleGuid = (Get-Module $ModuleName).Guid
                    $CmdletNames = GetCommands -AsNames -Module $moduleName
                }
                else { # 'FromMaml'

                    if (-not (Test-Path $_)) {
                        throw "No file found in $_."
                    }

                    GetMamlObject -MamlFile $_ -ConvertNotesToList:$ConvertNotesToList -ConvertDoubleDashLists:$ConvertDoubleDashLists | processMamlObjectToFile

                    $CmdletNames += GetMamlObject -MamlFile $_ | ForEach-Object {$_.Name}
                }

                if($WithModulePage) {
                    if(-not $ModuleGuid) {
                        $ModuleGuid = "00000000-0000-0000-0000-000000000000"
                    }
                    if($ModuleGuid.Count -gt 1) {
                        Write-Warning -Message "This module has more than 1 guid. This could impact external help creation."
                    }
                    # yeild
                    NewModuleLandingPage  -Path $OutputFolder `
                                        -ModuleName $ModuleName `
                                        -ModuleGuid $ModuleGuid `
                                        -CmdletNames $CmdletNames `
                                        -Locale $Locale `
                                        -Version $HelpVersion `
                                        -FwLink $FwLink `
                                        -Encoding $Encoding `
                                        -Force:$Force
                }
            }
        }
    }

    end {
        $perfTrace.Stop();

        Write-Verbose -Message ("[New-MarkdownHelp][End] [$($perfTrace.ElapsedMilliseconds)]");
    }
}

function Get-MarkdownMetadata
{
    [CmdletBinding(DefaultParameterSetName="FromPath")]
    param(
        [Parameter(Mandatory=$true,
            ValueFromPipeline=$true,
            ValueFromPipelineByPropertyName=$true,
            Position=1,
            ParameterSetName="FromPath")]
        [SupportsWildcards()]
        [string[]]$Path,

        [Parameter(Mandatory=$true, ParameterSetName="FromMarkdownString")]
        [string]$Markdown
    )

    begin {
        # Build a ToMetadata pipeline
        $pipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMetadata();
    }

    process
    {
        if ($PSCmdlet.ParameterSetName -eq 'FromMarkdownString') {
            return $pipeline.Process($Markdown, [String]::Empty);
        }
        # FromFile
        else {
            return GetMarkdownFilesFromPath -Path $Path -IncludeModulePage | ForEach-Object -Process {
                $pipeline.Process($_.FullName, [System.Text.Encoding]::ASCII); # yield
            }
        }
    }

    end {
        $pipeline = $Null;
    }
}

function Update-MarkdownHelp
{
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo[]])]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [SupportsWildcards()]
        [string[]]$Path,

        [System.Text.Encoding]$Encoding = $script:UTF8_NO_BOM,

        [string]$LogPath,
        [switch]$LogAppend,
        [switch]$AlphabeticParamsOrder,
        [switch]$UseFullTypeName,
        [switch]$UpdateInputOutput,

        [System.Management.Automation.Runspaces.PSSession]$Session,

        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.MarkdownHelpOption]$Option
    )

    begin
    {
        validateWorkingProvider
        $infoCallback = GetInfoCallback $LogPath -Append:$LogAppend
        $targetPaths = New-Object -TypeName 'System.Collections.Generic.List[string]';

        $Option = New-MarkdownHelpOption -Option $Option;

        # Sort by parameter name
        if ($PSBoundParameters.ContainsKey('AlphabeticParamsOrder') -and $AlphabeticParamsOrder) {
            $Option.Markdown.ParameterSort = [Markdown.MAML.Configuration.ParameterSort]::Name;
        }
    }

    process
    {
        if ($Path -is [string]) {
            $targetPaths.Add($Path);
        } elseif ($Path -is [array]) {
            $targetPaths.AddRange($Path);
        }
    }

    end
    {
        function log {
            param(
                [string]$message,
                [switch]$warning
            )

            $message = "[Update-MarkdownHelp] $([datetime]::now) $message"
            if ($warning)
            {
                Write-Warning $message
            }

            $infoCallback.Invoke($message)
        }

        $markdownFiles = GetMarkdownFile -Path $targetPaths;

        if ($Null -eq $markdownFiles -or $markdownFiles.Length -eq 0)
        {
            log -warning "No markdown found in $Path"
            return
        }

        # Build pipeline for reading markdown
        $readPipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMamlCommand($Option).Configure({
            param($config)

            $config.UsePreserveFormatting();
            $config.UseSchema();
        }).Build();

        # Build a pipeline for writing markdown
        $writePipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMarkdown($Option).Configure({
            param ($config)

            $config.SetOnlineVersionUrl();
            $config.UsePreserveFormatting();
        }).Build();

        foreach ($file in $markdownFiles) {

            # Process the command markdown file
            $oldModel = $readPipeline.Process($file, $Encoding);

            # Discover the PS command that matches the name of the stored model
            $name = $oldModel.Name;
            $command = Get-Command -Name $name;

            if (!$command) {
                log -warning  "command $name not found in the session, skipping upgrade for $file";
                return
            }

            # update the help file entry in the metadata
            $metadata = Get-MarkdownMetadata -Path $file
            $metadata["external help file"] = GetHelpFileName $command
            $reflectionModel = GetMamlObject -Session $Session -Cmdlet $name -UseFullTypeName:$UseFullTypeName
            $metadata[$script:MODULE_PAGE_MODULE_NAME] = $reflectionModel.ModuleName

            $merger = New-Object Markdown.MAML.Transformer.MamlModelMerger -ArgumentList $infoCallback
            $newModel = $merger.Merge($reflectionModel, $oldModel, $UpdateInputOutput);

            # Update command help file
            $newModel.SetMetadata("external help file", (GetHelpFileName $command));
            $newModel.SetMetadata($script:MODULE_PAGE_MODULE_NAME, $reflectionModel.ModuleName);

            $md = $writePipeline.Process($newModel);

            MySetContent -path $file -value $md -Encoding $Encoding -Force # yield
        }
    }
}

function Merge-MarkdownHelp
{
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo[]])]
    param(
        [Parameter(Mandatory=$true,
            ValueFromPipeline=$true)]
        [SupportsWildcards()]
        [string[]]$Path,

        [Parameter(Mandatory=$true)]
        [string]$OutputPath,

        [System.Text.Encoding]$Encoding = $script:UTF8_NO_BOM,

        [Switch]$ExplicitApplicableIfAll,

        [Switch]$Force,

        [string]$MergeMarker = "!!! "
    )

    begin
    {
        validateWorkingProvider
        $MarkdownFiles = @()
    }

    process
    {
        $MarkdownFiles += GetMarkdownFilesFromPath -Path $Path;
    }

    end
    {
        function log
        {
            param(
                [string]$message,
                [switch]$warning
            )

            $message = "[Update-MarkdownHelp] $([datetime]::now) $message"
            if ($warning)
            {
                Write-Warning $message
            }
            else
            {
                Write-Verbose $message
            }
        }

        if (-not $MarkdownFiles)
        {
             log -warning "No markdown found in $Path"
            return
        }

        function getTags
        {
            param($files)

            ($files | Split-Path | Split-Path -Leaf | Group-Object).Name
        }

        # use parent folder names as tags
        $allTags = getTags $MarkdownFiles
        log "Using following tags for the merge: $tags"
        $fileGroups = $MarkdownFiles | Group-Object -Property Name
        log "Found $($fileGroups.Count) file groups"

        $fileGroups | ForEach-Object {
            $files = $_.Group
            $groupName = $_.Name

            $dict = New-Object 'System.Collections.Generic.Dictionary[string, Markdown.MAML.Model.MAML.MamlCommand]'
            $files | ForEach-Object {
                $model = GetMamlModelImpl $_.FullName -ForAnotherMarkdown -Encoding $Encoding
                # unwrap List of 1 element
                $model = $model[0]
                $tag = getTags $_
                log "Adding tag $tag and $model"
                $dict[$tag] = $model
            }

            $tags = $dict.Keys
            if (($allTags | measure-object).Count -gt ($tags | measure-object).Count -or $ExplicitApplicableIfAll)
            {
                $newMetadata = @{ $script:APPLICABLE_YAML_HEADER = $tags -join ', ' }
            }
            else
            {
                $newMetadata = @{}
            }

            $merger = New-Object Markdown.MAML.Transformer.MamlMultiModelMerger -ArgumentList $null, (-not $ExplicitApplicableIfAll), $MergeMarker
            $newModel = $merger.Merge($dict)

            $md = ConvertMamlModelToMarkdown -mamlCommand $newModel -metadata $newMetadata -PreserveFormatting
            $outputFilePath = Join-Path $OutputPath $groupName
            MySetContent -path $outputFilePath -value $md -Encoding $Encoding -Force:$Force # yeild
        }
    }
}

function Update-MarkdownHelpModule
{
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo[]])]
    param(
        [Parameter(Mandatory=$true,
            ValueFromPipeline=$true)]
        [SupportsWildcards()]
        [string[]]$Path,

        [System.Text.Encoding]$Encoding = $script:UTF8_NO_BOM,
        [switch]$RefreshModulePage,
        [string]$LogPath,
        [switch]$LogAppend,
        [switch]$AlphabeticParamsOrder,
        [switch]$UseFullTypeName,
        [switch]$UpdateInputOutput,

        [System.Management.Automation.Runspaces.PSSession]$Session
    )

    begin
    {
        validateWorkingProvider
        $infoCallback = GetInfoCallback $LogPath -Append:$LogAppend
    }

    end
    {
        function log
        {
            param(
                [string]$message,
                [switch]$warning
            )

            $message = "[Update-MarkdownHelpModule] $([datetime]::now) $message"
            if ($warning)
            {
                Write-Warning $message
            }

            $infoCallback.Invoke($message)
        }

        foreach ($modulePath in $Path)
        {
            $module = $null
            $h = Get-MarkdownMetadata -Path $modulePath
            # this is pretty hacky and would lead to errors
            # the idea is to find module name from landing page when it's available
            if ($h.$script:MODULE_PAGE_MODULE_NAME)
            {
                $module = $h.$script:MODULE_PAGE_MODULE_NAME | Select-Object -First 1
                log "Determined module name for $modulePath as $module"
            }

            if (-not $module)
            {
                Write-Error "Cannot determine module name for $modulePath. You should use New-MarkdownHelp -WithModulePage to create HelpModule"
                continue
            }

            # always append on this call
            log ("[Update-MarkdownHelpModule]" + (Get-Date).ToString())
            log ("Updating docs for Module " + $module + " in " + $modulePath)
            $affectedFiles = Update-MarkdownHelp -Session $Session -Path $modulePath -LogPath $LogPath -LogAppend -Encoding $Encoding -AlphabeticParamsOrder:$AlphabeticParamsOrder -UseFullTypeName:$UseFullTypeName -UpdateInputOutput:$UpdateInputOutput
            $affectedFiles # yeild

            $allCommands = GetCommands -AsNames -Module $Module
            if (-not $allCommands)
            {
                throw "Module $Module is not imported in the session or doesn't have any exported commands"
            }

            $updatedCommands = $affectedFiles.BaseName
            $allCommands | ForEach-Object {
                if ( -not ($updatedCommands -contains $_) )
                {
                    log "Creating new markdown for command $_"
                    $newFiles = New-MarkdownHelp -Command $_ -OutputFolder $modulePath -AlphabeticParamsOrder:$AlphabeticParamsOrder
                    $newFiles # yeild
                }
            }

            if($RefreshModulePage)
            {
                $MamlModel = New-Object System.Collections.Generic.List[Markdown.MAML.Model.MAML.MamlCommand]
                $MamlModel = GetMamlModelImpl $affectedFiles -ForAnotherMarkdown -Encoding $Encoding
                NewModuleLandingPage  -RefreshModulePage -Path $modulePath -ModuleName $module -Module $MamlModel -Encoding $Encoding -Force
            }
        }
    }
}

function New-MarkdownAboutHelp
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string] $OutputFolder,
        [string] $AboutName
    )

    begin
    {
        if ($AboutName.StartsWith('about_')) { $AboutName = $AboutName.Substring('about_'.Length)}
        validateWorkingProvider
        $templatePath =  Join-Path $PSScriptRoot "templates\aboutTemplate.md"
    }

    process
    {
        if(Test-Path $OutputFolder)
        {
            $AboutContent = Get-Content $templatePath
            $AboutContent = $AboutContent.Replace("{{FileNameForHelpSystem}}",("about_" + $AboutName))
            $AboutContent = $AboutContent.Replace("{{TOPIC NAME}}",$AboutName)
            $NewAboutTopic = New-Item -Path $OutputFolder -Name "about_$($AboutName).md"
            Set-Content -Value $AboutContent -Path $NewAboutTopic -Encoding UTF8
        }
        else
        {
            throw "The output folder does not exist."
        }
    }
}

function New-YamlHelp
{
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo[]])]
    param(
        [Parameter(Mandatory=$true,
            Position=1,
            ValueFromPipeline=$true,
            ValueFromPipelineByPropertyName=$true)]
        [string[]]$Path,

        [Parameter(Mandatory=$true)]
        [string]$OutputFolder,

        [System.Text.Encoding]$Encoding = [System.Text.Encoding]::UTF8,

        [switch]$Force
    )
    begin
    {
        validateWorkingProvider

        $MarkdownFiles = @()

        if(-not (Test-Path $OutputFolder))
        {
            $Null = New-Item -Type Directory $OutputFolder -ErrorAction SilentlyContinue
        }

        if(-not (Test-Path -PathType Container $OutputFolder))
        {
            throw "$OutputFolder is not a container"
        }
    }
    process
    {
        $MarkdownFiles += GetMarkdownFilesFromPath -Path $Path;
    }
    end
    {
        $MarkdownFiles | ForEach-Object {
            Write-Verbose "[New-YamlHelp] Input markdown file $_"
        }

        foreach($markdownFile in $MarkdownFiles)
        {
            $mamlModels = GetMamlModelImpl $markdownFile.FullName -Encoding $Encoding
            foreach($mamlModel in $mamlModels)
            {
                $markdownMetadata = Get-MarkdownMetadata -Path $MarkdownFile.FullName

                ## We set the module here in the PowerShell since the Yaml block is not read by the parser
                $mamlModel.ModuleName = $markdownMetadata[$script:MODULE_PAGE_MODULE_NAME]

                $yaml = [Markdown.MAML.Renderer.YamlRenderer]::MamlModelToString($mamlModel)
                $outputFilePath = Join-Path $OutputFolder ($mamlModel.Name + ".yml")
                Write-Verbose "Writing Yaml help to $outputFilePath"
                MySetContent -Path $outputFilePath -Value $yaml -Encoding $Encoding -Force:$Force
            }
        }
    }
}

function New-ExternalHelp
{
    [CmdletBinding()]
    [OutputType([System.IO.FileInfo[]])]
    param(
        [Parameter(Mandatory=$true,
            Position=1,
            ValueFromPipeline=$true,
            ValueFromPipelineByPropertyName=$true)]
        [SupportsWildcards()]
        [string[]]$Path,

        [Parameter(Mandatory=$true)]
        [string]$OutputPath,

        [string[]]$ApplicableTag,

        [System.Text.Encoding]$Encoding = [System.Text.Encoding]::UTF8,

        # TODO: Help generation does not include this validate range in help
        [ValidateRange(80, [int]::MaxValue)]
        [int]$MaxAboutWidth = 80,

        [string]$ErrorLogFile,

        [switch]$Force,
        
        [switch]$ShowProgress,

        # TODO: Option is created as a positional parameter instead of named
        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.MarkdownHelpOption]$Option
    )

    begin
    {
        $perfTrace = New-Object -TypeName System.Diagnostics.Stopwatch;
        $perfTrace.Start();

        validateWorkingProvider

        $Option = New-MarkdownHelpOption -Option $Option;

        $MarkdownFiles = @()
        $AboutFiles = @()
        $IsOutputContainer = $true
        if ( $OutputPath.EndsWith('.xml') -and (-not (Test-Path -PathType Container $OutputPath )) )
        {
            $IsOutputContainer = $false
            Write-Verbose "[New-ExternalHelp] Use $OutputPath as path to a file"
        }
        else
        {
            New-Item -Type Directory $OutputPath -ErrorAction SilentlyContinue > $null
            Write-Verbose "[New-ExternalHelp] Use $OutputPath as path to a directory"
        }

        Write-Verbose -Message ("[New-ExternalHelp][Start] Adding files [$($perfTrace.ElapsedMilliseconds)]");

        if ( -not $ShowProgress.IsPresent -or $(Get-Variable -Name IsCoreClr -ValueOnly -ErrorAction SilentlyContinue) )
        {
            Function Write-Progress() {}
        }
        Write-Verbose -Message ("[New-ExternalHelp][Start] Adding files [$($perfTrace.ElapsedMilliseconds)]");

        if ( -not $ShowProgress.IsPresent -or $(Get-Variable -Name IsCoreClr -ValueOnly -ErrorAction SilentlyContinue) )
        {
            Function Write-Progress() {}
        }
    }

    process
    {
        $MarkdownFiles += GetMarkdownFilesFromPath -Path $Path;

        if($MarkdownFiles)
        {
            $AboutFiles += GetAboutTopicsFromPath -Path $Path -MarkDownFilesAlreadyFound $MarkdownFiles.FullName
        }
        else
        {
            $AboutFiles += GetAboutTopicsFromPath -Path $Path
        }
    }

    end
    {
        Write-Verbose -Message ("[New-ExternalHelp][Stop] Adding files [$($perfTrace.ElapsedMilliseconds)]");

        # Tracks all warnings and errors 
        $warningsAndErrors = New-Object System.Collections.Generic.List[System.Object]

        try {
            # write verbose output and filter out files based on applicable tag
            $MarkdownFiles | ForEach-Object {
                Write-Verbose "[New-ExternalHelp] Input markdown file $_"
            }

            if ($ApplicableTag) {
                Write-Verbose "[New-ExternalHelp] Filtering for ApplicableTag $ApplicableTag"
                $MarkdownFiles = $MarkdownFiles | ForEach-Object {
                    $applicableList = GetApplicableList -Path $_.FullName
                    # this Compare-Object call is getting the intersection of two string[]
                    if ((-not $applicableList) -or (Compare-Object $applicableList $ApplicableTag -IncludeEqual -ExcludeDifferent)) {
                        # yield
                        $_
                    }
                    else {
                        Write-Verbose "[New-ExternalHelp] Skipping markdown file $_"
                    }
                }
            }

            # group the files based on the output xml path metadata tag
            if ($IsOutputContainer) {
                $defaultPath = Join-Path $OutputPath $script:DEFAULT_MAML_XML_OUTPUT_NAME
                $groups = $MarkdownFiles | Group-Object { 
                    $h = Get-MarkdownMetadata -Path $_.FullName
                    if ($h -and $h[$script:EXTERNAL_HELP_FILE_YAML_HEADER]) {
                        Join-Path $OutputPath $h[$script:EXTERNAL_HELP_FILE_YAML_HEADER]
                    }
                    else {
                        $msgLine1 = "cannot find '$($script:EXTERNAL_HELP_FILE_YAML_HEADER)' in metadata for file $($_.FullName)"
                        $msgLine2 = "$defaultPath would be used"
                        $warningsAndErrors.Add(@{
                            Severity = "Warning"
                            Message  = "$msgLine1 $msgLine2"
                            FilePath = "$($_.FullName)"
                        })

                        Write-Warning "[New-ExternalHelp] $msgLine1"
                        Write-Warning "[New-ExternalHelp] $msgLine2"
                        $defaultPath
                    }
                }
            }
            else {
                $groups = $MarkdownFiles | Group-Object { $OutputPath }
            }

            # Create a pipeline to render MAML XML
            $mamlPipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMamlXml($Option).Configure({
                param($config)
                $config.UseApplicableTag($ApplicableTag);
                $config.UseSchema();
            }).Build();

            # Generate XML content a group at a time
            foreach ($group in $groups) {
                $outPath = $group.Name;

                Write-Verbose -Message ("[New-ExternalHelp][Start] Processing $outPath [$($perfTrace.ElapsedMilliseconds)]");

                $xml = $mamlPipeline.Process($group.Group.FullName, $Encoding);

                Write-Verbose "Writing external help to $outPath"
                MySetContent -Path $outPath -Value $xml -Encoding $Encoding -Force:$Force;

                Write-Verbose -Message ("[New-ExternalHelp][Stop] Processing $outPath [$($perfTrace.ElapsedMilliseconds)]");
            }

            # Create a pipeline to render about topics
            $aboutPipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToAboutText();
        
            # handle about topics
            if ($AboutFiles.Count -gt 0) {
                foreach ($About in $AboutFiles) {

                    # Process the about topic
                    $text = $aboutPipeline.Process($About.FullName, $Encoding);

                    $outPath = Join-Path $OutputPath ([io.path]::GetFileNameWithoutExtension($About.FullName) + ".help.txt")
                    if (!(Split-Path -Leaf $outPath).ToUpper().StartsWith("ABOUT_", $true, $null)) {
                        $outPath = Join-Path (Split-Path -Parent $outPath) ("about_" + (Split-Path -Leaf $outPath))
                    }
                    MySetContent -Path $outPath -Value $text -Encoding $Encoding -Force:$Force
                }
            }
        }
        catch {
            # Log error and rethrow
            $warningsAndErrors.Add(@{
                Severity = "Error"
                Message  = "$_.Exception.Message"
                FilePath = ""
                })

            throw
        }
        finally {
            if ($ErrorLogFile) {
                ConvertTo-Json $warningsAndErrors | Out-File $ErrorLogFile
            }
        }

        $perfTrace.Stop();
    }
}

function Get-HelpPreview
{
    [CmdletBinding()]
    [OutputType('MamlCommandHelpInfo')]
    param(
        [Parameter(Mandatory=$true,
            ValueFromPipeline=$true,
            Position=1)]
        [SupportsWildcards()]
        [string[]]$Path,

        [switch]$ConvertNotesToList,
        [switch]$ConvertDoubleDashLists
    )

    process
    {
        foreach ($MamlFilePath in $Path)
        {
            if (-not (Test-path -Type Leaf $MamlFilePath))
            {
                Write-Error "$MamlFilePath is not found, skipping"
                continue
            }

            # this is Resolve-Path that resolves mounted drives (i.e. good for tests)
            $MamlFilePath = (Get-ChildItem $MamlFilePath).FullName

            # Read the maml file
            $xml = [xml](Get-Content $MamlFilePath -Raw -ea SilentlyContinue)
            if (-not $xml)
            {
                # already error-out on the convertion, no need to repeat ourselves
                continue
            }

            # we need a copy of maml file to bypass powershell cache,
            # in case we reuse the same filename few times.
            $MamlCopyPath = [System.IO.Path]::GetTempFileName()
            try
            {
                if ($ConvertDoubleDashLists)
                {
                    $Null = $xml.GetElementsByTagName('maml:para') | ForEach-Object {
                        # Convert "-- "-lists into "- "-lists
                        # to make them markdown compatible
                        # as described in https://github.com/PowerShell/platyPS/issues/117
                        $newInnerXml = $_.get_InnerXml() -replace "(`n|^)-- ", '$1- '
                        $_.set_InnerXml($newInnerXml)
                    }
                }

                if ($ConvertNotesToList)
                {
                    # Add inline bullet-list, as described in https://github.com/PowerShell/platyPS/issues/125
                    $xml.helpItems.command.alertSet.alert |
                        ForEach-Object {
                            # make first <para> a list item
                            # add indentations to other <para> to make them continuation of list item
                            $_.ChildNodes | Select-Object -First 1 |
                            ForEach-Object {
                                $newInnerXml = '* ' + $_.get_InnerXml()
                                $_.set_InnerXml($newInnerXml)
                            }

                            $_.ChildNodes | Select-Object -Skip 1 |
                            ForEach-Object {
                                # this character is not a valid space.
                                # We have to use some odd character here, becasue help engine strips out
                                # all legetimate whitespaces.
                                # Note: powershell doesn't render it properly, it will appear as a non-writable char.
                                $newInnerXml = ([string][char]0xc2a0) * 2 + $_.get_InnerXml()
                                $_.set_InnerXml($newInnerXml)
                            }
                        }
                }

                # in PS v5 help engine is not happy, when first non-empty link (== Online version link) is not a valid URI
                # User encounter this problem too oftern to ignore it, hence this workaround in platyPS:
                # always add a dummy link with a valid URI into xml and then remove the first link from the help object.
                # for more context see https://github.com/PowerShell/platyPS/issues/144
                $xml.helpItems.command.relatedLinks | ForEach-Object {
                    if ($_)
                    {
                        $_.InnerXml = '<maml:navigationLink xmlns:maml="http://schemas.microsoft.com/maml/2004/10"><maml:linkText>PLATYPS_DUMMY_LINK</maml:linkText><maml:uri>https://github.com/PowerShell/platyPS/issues/144</maml:uri></maml:navigationLink>' + $_.InnerXml
                    }
                }

                $xml.Save($MamlCopyPath)

                foreach ($command in $xml.helpItems.command.details.name)
                {
                    #PlatyPS will have trouble parsing a command with space around the name.
                    $command = $command.Trim()
                    $thisDefinition = @"

<#
.ExternalHelp $MamlCopyPath
#>
filter $command
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=`$true)]
        [switch]`$platyPSHijack
    )

    Microsoft.PowerShell.Utility\Write-Warning 'PlatyPS hijacked your command $command.'
    Microsoft.PowerShell.Utility\Write-Warning 'We are sorry for that. It means, there is a bug in our Get-HelpPreview logic.'
    Microsoft.PowerShell.Utility\Write-Warning 'Please report this issue https://github.com/PowerShell/platyPS/issues'
    Microsoft.PowerShell.Utility\Write-Warning 'Restart PowerShell to fix the problem.'
}

# filter is rare enough to distinguish with other commands
`$innerHelp = Microsoft.PowerShell.Core\Get-Help $command -Full -Category filter

Microsoft.PowerShell.Core\Export-ModuleMember -Function @()
"@
                    $m = New-Module ( [scriptblock]::Create( "$thisDefinition" ))
                    $help = & $m { $innerHelp }
                    # this is the second part of the workaround for https://github.com/PowerShell/platyPS/issues/144
                    # see comments above for context
                    $help.relatedLinks | ForEach-Object {
                        if ($_)
                        {
                            $_.navigationLink = $_.navigationLink | Select-Object -Skip 1
                        }
                    }
                    $help # yeild
                }
            }
            finally
            {
                Remove-Item $MamlCopyPath
            }
        }
    }
}

function New-ExternalHelpCab
{
    [Cmdletbinding()]
    param(
        [parameter(Mandatory=$true)]
        [ValidateScript(
            {
                if(Test-Path $_ -PathType Container)
                {
                    $True
                }
                else
                {
                    Throw "$_ content source file folder path is not a valid directory."
                }
            })]
        [string] $CabFilesFolder,
        [parameter(Mandatory=$true)]
        [ValidateScript(
            {
                if(Test-Path $_ -PathType Leaf)
                {
                    $True
                }
                else
                {
                    Throw "$_ Module Landing Page path is nopt valid."
                }
            })]
        [string] $LandingPagePath,
        [parameter(Mandatory=$true)]
        [string] $OutputFolder,

        [parameter()]
        [switch] $IncrementHelpVersion
    )
    begin
    {
        validateWorkingProvider
        New-Item -Type Directory $OutputFolder -ErrorAction SilentlyContinue > $null
    }
    process
    {
        #Testing for MakeCab.exe
        Write-Verbose "Testing that MakeCab.exe is present on this machine."
        $MakeCab = Get-Command MakeCab
        if(-not $MakeCab)
        {
            throw "MakeCab.exe is not a registered command."
        }
        #Testing for files in source directory
        if((Get-ChildItem -Path $CabFilesFolder).Count -le 0)
        {
            throw "The file count in the cab files directory is zero."
        }


    ###Get Yaml Metadata here
    $Metadata = Get-MarkdownMetadata -Path $LandingPagePath

    $ModuleName = $Metadata[$script:MODULE_PAGE_MODULE_NAME]
    $Guid = $Metadata[$script:MODULE_PAGE_GUID]
    $Locale = $Metadata[$script:MODULE_PAGE_LOCALE]
    $FwLink = $Metadata[$script:MODULE_PAGE_FW_LINK]
    $OldHelpVersion = $Metadata[$script:MODULE_PAGE_HELP_VERSION]
    $AdditionalLocale = $Metadata[$script:MODULE_PAGE_ADDITIONAL_LOCALE]

    if($IncrementHelpVersion)
    {
        #IncrementHelpVersion
        $HelpVersion = IncrementHelpVersion -HelpVersionString $OldHelpVersion
        $MdContent = Get-Content -raw $LandingPagePath
        $MdContent = $MdContent.Replace($OldHelpVersion,$HelpVersion)
        Set-Content -path $LandingPagePath -value $MdContent
    }
    else
    {
        $HelpVersion = $OldHelpVersion
    }

    #Create HelpInfo File

        #Testing the destination directories, creating if none exists.
        Write-Verbose "Checking the output directory"
        if(-not (Test-Path $OutputFolder))
        {
            Write-Verbose "Output directory does not exist, creating a new directory."
            New-Item -ItemType Directory -Path $OutputFolder
        }

        Write-Verbose ("Creating cab for {0}, with Guid {1}, in Locale {2}" -f $ModuleName,$Guid,$Locale)

        #Building the cabinet file name.
        $cabName = ("{0}_{1}_{2}_HelpContent.cab" -f $ModuleName,$Guid,$Locale)
        $zipName = ("{0}_{1}_{2}_HelpContent.zip" -f $ModuleName,$Guid,$Locale)
        $zipPath = (Join-Path $OutputFolder $zipName)

        #Setting Cab Directives, make a cab is turned on, compression is turned on
        Write-Verbose "Creating Cab File"
        $DirectiveFile = "dir.dff"
        New-Item -ItemType File -Name $DirectiveFile -Force |Out-Null
        Add-Content $DirectiveFile ".Set Cabinet=on"
        Add-Content $DirectiveFile ".Set Compress=on"

        #Creates an entry in the cab directive file for each file in the source directory (uses FullName to get fuly qualified file path and name)
        foreach($file in Get-ChildItem -Path $CabFilesFolder -File)
        {
            Add-Content $DirectiveFile ("'" + ($file).FullName +"'" )
            Compress-Archive -DestinationPath $zipPath -Path $file.FullName -Update
        }

        #Making Cab
        Write-Verbose "Making the cab file"
        MakeCab.exe /f $DirectiveFile | Out-Null

        #Naming CabFile
        Write-Verbose "Moving the cab to the output directory"
        Copy-Item "disk1/1.cab" (Join-Path $OutputFolder $cabName)

        #Remove ExtraFiles created by the cabbing process
        Write-Verbose "Performing cabbing cleanup"
        Remove-Item "setup.inf" -ErrorAction SilentlyContinue
        Remove-Item "setup.rpt" -ErrorAction SilentlyContinue
        Remove-Item $DirectiveFile -ErrorAction SilentlyContinue
        Remove-Item -Path "disk1" -Recurse -ErrorAction SilentlyContinue

        #Create the HelpInfo Xml
        MakeHelpInfoXml -ModuleName $ModuleName -GUID $Guid -HelpCulture $Locale -HelpVersion $HelpVersion -URI $FwLink -OutputFolder $OutputFolder

        if($AdditionalLocale)
        {
            $allLocales = $AdditionalLocale -split ','

            foreach($loc in $allLocales)
            {
                #Create the HelpInfo Xml for each locale
                $locVersion = $Metadata["$loc Version"]

                if([String]::IsNullOrEmpty($locVersion))
                {
                    Write-Warning ("No version found for Locale: {0}" -f $loc)
                }
                else
                {
                    MakeHelpInfoXml -ModuleName $ModuleName -GUID $Guid -HelpCulture $loc -HelpVersion $locVersion -URI $FwLink -OutputFolder $OutputFolder
                }
            }
        }
    }
}

function New-MarkdownHelpOption {

    [CmdletBinding(DefaultParameterSetName = 'Option')]
    [OutputType([Markdown.MAML.Configuration.MarkdownHelpOption])]
    param (
        [Parameter(Mandatory = $False, ParameterSetName = 'Option')]
        [AllowNull()]
        [Markdown.MAML.Configuration.MarkdownHelpOption]$Option,

        [Parameter(Mandatory = $False, ParameterSetName = 'Path')]
        [PSDefaultValue(Help = '.')]
        [String]$Path = $PWD,

        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.VisitMarkdown[]]$ReadMarkdown,

        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.VisitMarkdown[]]$WriteMarkdown,

        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.MamlCommandScriptHook[]]$ReadCommand,

        [Parameter(Mandatory = $False)]
        [Markdown.MAML.Configuration.MamlCommandScriptHook[]]$WriteCommand
    )

    process {

        if ($PSBoundParameters.ContainsKey('Path')) {

            if (!(Test-Path -Path $Path)) {
                Write-Error -Message "Failed to read: $Path";

                return;
            }

            $Path = [Markdown.MAML.Configuration.MarkdownHelpOption]::GetYamlPath($Path);
            Write-Verbose -Message "Reading configuration from: $Path";
            $Option = [Markdown.MAML.Configuration.MarkdownHelpOption]::FromFile($Path, $True);
        }
        elseif (!$PSBoundParameters.ContainsKey('Option')) {
            # Generate an oject when path and option are not specified
            $Path = [Markdown.MAML.Configuration.MarkdownHelpOption]::GetYamlPath($Path);
            Write-Verbose -Message "Reading default configuration from: $Path";
            $Option = [Markdown.MAML.Configuration.MarkdownHelpOption]::FromFile($Path, $True);
        }

        if ($PSBoundParameters.ContainsKey('ReadMarkdown')) {
            Write-Verbose -Message "Set ReadMarkdown pipeline hook";
            $Option.Pipeline.ReadMarkdown.AddRange($ReadMarkdown);
        }

        if ($PSBoundParameters.ContainsKey('WriteMarkdown')) {
            Write-Verbose -Message "Set WriteMarkdown pipeline hook";
            $Option.Pipeline.WriteMarkdown.AddRange($WriteMarkdown);
        }

        if ($PSBoundParameters.ContainsKey('ReadCommand')) {
            Write-Verbose -Message "Set ReadCommand pipeline hook";
            $Option.Pipeline.ReadCommand.AddRange($ReadCommand);
        }

        if ($PSBoundParameters.ContainsKey('WriteCommand')) {
            Write-Verbose -Message "Set WriteCommand pipeline hook";
            $Option.Pipeline.WriteCommand.AddRange($WriteCommand);
        }

        return $Option;
    }
}

function Set-MarkdownHelpOption {

    [CmdletBinding(SupportsShouldProcess = $True)]
    param (
        [Parameter(Mandatory = $False, Position = 0)]
        [PSDefaultValue(Help = '.')]
        [String]$Path = $PWD,

        [Parameter(Mandatory = $False, ValueFromPipeline = $True)]
        [Markdown.MAML.Configuration.MarkdownHelpOption]$Option
    )

    process {
        
        if ($Null -eq $Option) {
            $Option = New-MarkdownHelpOption;
        }

        # Default to .platyps.yml if a directory is used
        $Path = [Markdown.MAML.Configuration.MarkdownHelpOption]::GetYamlPath($Path);

        # Get the parent directory instead
        $parentPath = Split-Path -Path $Path -Parent;

        # Create the parent path if it doesn't exist
        if (!(Test-Path -Path $parentPath)) {
            $Null = New-Item -Path $parentPath -ItemType Directory -Force -WhatIf:$WhatIfPreference;
        }

        MySetContent -Path $Path -value ($Option.ToYaml()) -Encoding $Script:UTF8_NO_BOM;
    }
}

#endregion

#region Implementation
# IIIIIIIIII                                            lllllll                                                                                            tttt                                    tttt            iiii
# I::::::::I                                            l:::::l                                                                                         ttt:::t                                 ttt:::t           i::::i
# I::::::::I                                            l:::::l                                                                                         t:::::t                                 t:::::t            iiii
# II::::::II                                            l:::::l                                                                                         t:::::t                                 t:::::t
#   I::::I     mmmmmmm    mmmmmmm   ppppp   ppppppppp    l::::l     eeeeeeeeeeee       mmmmmmm    mmmmmmm       eeeeeeeeeeee    nnnn  nnnnnnnn    ttttttt:::::ttttttt      aaaaaaaaaaaaa  ttttttt:::::ttttttt    iiiiiii    ooooooooooo   nnnn  nnnnnnnn
#   I::::I   mm:::::::m  m:::::::mm p::::ppp:::::::::p   l::::l   ee::::::::::::ee   mm:::::::m  m:::::::mm   ee::::::::::::ee  n:::nn::::::::nn  t:::::::::::::::::t      a::::::::::::a t:::::::::::::::::t    i:::::i  oo:::::::::::oo n:::nn::::::::nn
#   I::::I  m::::::::::mm::::::::::mp:::::::::::::::::p  l::::l  e::::::eeeee:::::eem::::::::::mm::::::::::m e::::::eeeee:::::een::::::::::::::nn t:::::::::::::::::t      aaaaaaaaa:::::at:::::::::::::::::t     i::::i o:::::::::::::::on::::::::::::::nn
#   I::::I  m::::::::::::::::::::::mpp::::::ppppp::::::p l::::l e::::::e     e:::::em::::::::::::::::::::::me::::::e     e:::::enn:::::::::::::::ntttttt:::::::tttttt               a::::atttttt:::::::tttttt     i::::i o:::::ooooo:::::onn:::::::::::::::n
#   I::::I  m:::::mmm::::::mmm:::::m p:::::p     p:::::p l::::l e:::::::eeeee::::::em:::::mmm::::::mmm:::::me:::::::eeeee::::::e  n:::::nnnn:::::n      t:::::t              aaaaaaa:::::a      t:::::t           i::::i o::::o     o::::o  n:::::nnnn:::::n
#   I::::I  m::::m   m::::m   m::::m p:::::p     p:::::p l::::l e:::::::::::::::::e m::::m   m::::m   m::::me:::::::::::::::::e   n::::n    n::::n      t:::::t            aa::::::::::::a      t:::::t           i::::i o::::o     o::::o  n::::n    n::::n
#   I::::I  m::::m   m::::m   m::::m p:::::p     p:::::p l::::l e::::::eeeeeeeeeee  m::::m   m::::m   m::::me::::::eeeeeeeeeee    n::::n    n::::n      t:::::t           a::::aaaa::::::a      t:::::t           i::::i o::::o     o::::o  n::::n    n::::n
#   I::::I  m::::m   m::::m   m::::m p:::::p    p::::::p l::::l e:::::::e           m::::m   m::::m   m::::me:::::::e             n::::n    n::::n      t:::::t    tttttta::::a    a:::::a      t:::::t    tttttt i::::i o::::o     o::::o  n::::n    n::::n
# II::::::IIm::::m   m::::m   m::::m p:::::ppppp:::::::pl::::::le::::::::e          m::::m   m::::m   m::::me::::::::e            n::::n    n::::n      t::::::tttt:::::ta::::a    a:::::a      t::::::tttt:::::ti::::::io:::::ooooo:::::o  n::::n    n::::n
# I::::::::Im::::m   m::::m   m::::m p::::::::::::::::p l::::::l e::::::::eeeeeeee  m::::m   m::::m   m::::m e::::::::eeeeeeee    n::::n    n::::n      tt::::::::::::::ta:::::aaaa::::::a      tt::::::::::::::ti::::::io:::::::::::::::o  n::::n    n::::n
# I::::::::Im::::m   m::::m   m::::m p::::::::::::::pp  l::::::l  ee:::::::::::::e  m::::m   m::::m   m::::m  ee:::::::::::::e    n::::n    n::::n        tt:::::::::::tt a::::::::::aa:::a       tt:::::::::::tti::::::i oo:::::::::::oo   n::::n    n::::n
# IIIIIIIIIImmmmmm   mmmmmm   mmmmmm p::::::pppppppp    llllllll    eeeeeeeeeeeeee  mmmmmm   mmmmmm   mmmmmm    eeeeeeeeeeeeee    nnnnnn    nnnnnn          ttttttttttt    aaaaaaaaaa  aaaa         ttttttttttt  iiiiiiii   ooooooooooo     nnnnnn    nnnnnn
#                                    p:::::p
#                                    p:::::p
#                                   p:::::::p
#                                   p:::::::p
#                                   p:::::::p
#                                   ppppppppp

# parse out the list "applicable" tags from yaml header
function GetApplicableList
{
    param(
        [Parameter(Mandatory=$true)]
        $Path
    )

    $h = Get-MarkdownMetadata -Path $Path
    if ($h -and $h[$script:APPLICABLE_YAML_HEADER]) {
        return $h[$script:APPLICABLE_YAML_HEADER].Split(',').Trim()
    }
}

# If LogPath not provided, use -Verbose output for logs
function GetInfoCallback
{
    param(
        [string]$LogPath,
        [switch]$Append
    )

    if ($LogPath)
    {
        if (-not (Test-Path $LogPath -PathType Leaf))
        {
            $containerFolder = Split-Path $LogPath
            if ($containerFolder)
            {
                # this if is for $LogPath -eq foo.log  case
                New-Item -Type Directory $containerFolder -ErrorAction SilentlyContinue > $null
            }

            if (-not $Append)
            {
                # wipe the file, so it can be reused
                Set-Content -Path $LogPath -value '' -Encoding UTF8
            }
        }

        $infoCallback = {
            param([string]$message)
            Add-Content -Path $LogPath -value $message -Encoding UTF8
        }
    }
    else
    {
        $infoCallback = {
            param([string]$message)
            Write-Verbose $message
        }
    }
    return $infoCallback
}

function GetWarningCallback
{
    $warningCallback = {
        param([string]$message)
        Write-Warning $message
    }

    return $warningCallback
}

function IsAboutTopic
{
    [OutputType([System.Boolean])]
    param(
        [string]$Path
    )

    $MdContent = Get-Content -raw $Path

    $topic = [Markdown.MAML.Pipeline.PipelineBuilder]::ToAboutTopic().Process($MdContent, $Path);

    # $MdParser = new-object -TypeName 'Markdown.MAML.Parser.MarkdownParser' `
    #                         -ArgumentList { param([int]$current, [int]$all) 
    #                         Write-Progress -Activity "Parsing markdown" -status "Progress:" -percentcomplete ($current/$all*100)}
    # $MdObject = $MdParser.ParseString($MdContent)

    # if($MdObject.Children[1].text.length -gt 5)
    # {
    #     if($MdObject.Children[1].text.substring(0,5).ToUpper() -eq "ABOUT")
    #     {
    #         return $true
    #     }
    # }

    if ($topic.Name -like "about_*") {
        return $True;
    }

    return $false
}

function GetAboutTopicsFromPath
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$Path,
        [string[]]$MarkDownFilesAlreadyFound
    )

    function ConfirmAboutBySecondHeaderText
    {
        param(
            [string]$AboutFilePath
        )

        $MdContent = Get-Content -raw $AboutFilePath
        $MdParser = new-object -TypeName 'Markdown.MAML.Parser.MarkdownParser' `
                                -ArgumentList { param([int]$current, [int]$all)
                                Write-Progress -Activity "Parsing markdown" -status "Progress:" -percentcomplete ($current/$all*100)}
        $MdObject = $MdParser.ParseString($MdContent)

        if($MdObject.Children[1].text.length -gt 5)
        {
            if($MdObject.Children[1].text.substring(0,5).ToUpper() -eq "ABOUT")
            {
                return $true
            }
        }

        return $false
    }

    $AboutMarkDownFiles = @()

    if ($Path) {
        $Path | ForEach-Object {
            if (Test-Path -PathType Leaf $_)
            {
                if(IsAboutTopic -Path $_)
                {
                    $AboutMarkdownFiles += Get-ChildItem $_
                }
            }
            elseif (Test-Path -PathType Container $_)
            {
                if($MarkDownFilesAlreadyFound)
                {
                    $AboutMarkdownFiles += Get-ChildItem $_ -Filter '*.md' | Where-Object {($_.FullName -notin $MarkDownFilesAlreadyFound) -and (IsAboutTopic -Path $_.FullName)}
                }
                else
                {
                    $AboutMarkdownFiles += Get-ChildItem $_ -Filter '*.md' | Where-Object { (IsAboutTopic -Path $_.FullName) }
                }
            }
            else
            {
                Write-Error "$_ about file not found"
            }
        }
    }
    return $AboutMarkDownFiles
}

function GetMarkdownFile {

    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory = $True)]
        [string[]]$Path,

        [switch]$IncludeModulePage
    )

    process {

        $filter = '*-*.md'

        if ($IncludeModulePage) {
            $filter = '*.md'
        }

        $aboutFilePrefixPattern = 'about_*'

        return @(foreach ($p in $Path) {

            if (Test-Path -Path $p) {
                foreach ($i in (Get-ChildItem -Path $p -Filter $filter)) {

                    if (![String]::IsNullOrEmpty($i.FullName) -and $i.BaseName -notlike $aboutFilePrefixPattern) {
                        $i.FullName; #yield
                    }
                }
            } else {
                Write-Error "$p is not found"
            }
        })
    }
}

function GetMarkdownFilesFromPath
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [SupportsWildcards()]
        [string[]]$Path,

        [switch]$IncludeModulePage
    )

    if (!$Path) {
        return @();
    }

    $filter = '*-*.md'

    if ($IncludeModulePage)
    {
        $filter = '*.md'
    }

    $aboutFilePrefixPattern = 'about_*';

    return ($Path | ForEach-Object -Process {
        if (Test-Path -PathType Leaf -Path $_) {
            if ((Split-Path -Leaf $_) -notlike $aboutFilePrefixPattern) {
                Get-ChildItem -Path $_;
            }
        }
    })
}

function GetMamlModelImpl
{
    [OutputType([Markdown.MAML.Model.MAML.MamlCommand])]
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$markdownFiles,
        [Parameter(Mandatory=$true)]
        [System.Text.Encoding]$Encoding,
        [switch]$ForAnotherMarkdown,
        [String[]]$ApplicableTag
    )

    if ($ForAnotherMarkdown -and $ApplicableTag) {
        throw '[ASSERT] Incorrect usage: cannot pass both -ForAnotherMarkdown and -ApplicableTag'
    }

    $pipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMamlCommand().Configure({
        param($config)
        $config.SetOnlineVersionUrlLink();
        $config.UseApplicableTag($ApplicableTag);

        if ($ForAnotherMarkdown) {
            $config.UsePreserveFormatting();
        }

        $config.UseSchema();
    }).Build();

    foreach ($file in $markdownFiles)
    {
        $pipeline.Process($file, $Encoding);
    }
}

function NewMarkdownParser
{
    return [Markdown.MAML.Markdown]::GetParser();
}

function MakeHelpInfoXml
{
    Param(
        [Parameter(mandatory=$true)]
        [string]
        $ModuleName,
        [Parameter(mandatory=$true)]
        [string]
        $GUID,
        [Parameter(mandatory=$true)]
        [string]
        $HelpCulture,
        [Parameter(mandatory=$true)]
        [string]
        $HelpVersion,
        [Parameter(mandatory=$true)]
        [string]
        $URI,
        [Parameter(mandatory=$true)]
        [string]
        $OutputFolder


    )

    $HelpInfoFileNme = $ModuleName + "_" + $GUID + "_HelpInfo.xml"
    $OutputFullPath = Join-Path $OutputFolder $HelpInfoFileNme

    if(Test-Path $OutputFullPath -PathType Leaf)
    {
        [xml] $HelpInfoContent = Get-Content $OutputFullPath
    }

    #Create the base XML object for the Helpinfo.xml file.
    $xml = new-object xml

    $ns = "http://schemas.microsoft.com/powershell/help/2010/05"
    $declaration = $xml.CreateXmlDeclaration("1.0","utf-8",$null)

    $rootNode = $xml.CreateElement("HelpInfo",$ns)
    $xml.InsertBefore($declaration,$xml.DocumentElement)
    $xml.AppendChild($rootNode)

    $HelpContentUriNode = $xml.CreateElement("HelpContentURI",$ns)
    $HelpContentUriNode.InnerText = $URI
    $xml["HelpInfo"].AppendChild($HelpContentUriNode)

    $HelpSupportedCulturesNode = $xml.CreateElement("SupportedUICultures",$ns)
    $xml["HelpInfo"].AppendChild($HelpSupportedCulturesNode)


    #If no previous help file
    if(-not $HelpInfoContent)
    {
        $HelpUICultureNode = $xml.CreateElement("UICulture",$ns)
        $xml["HelpInfo"]["SupportedUICultures"].AppendChild($HelpUICultureNode)

        $HelpUICultureNameNode = $xml.CreateElement("UICultureName",$ns)
        $HelpUICultureNameNode.InnerText = $HelpCulture
        $xml["HelpInfo"]["SupportedUICultures"]["UICulture"].AppendChild($HelpUICultureNameNode)

        $HelpUICultureVersionNode = $xml.CreateElement("UICultureVersion",$ns)
        $HelpUICultureVersionNode.InnerText = $HelpVersion
        $xml["HelpInfo"]["SupportedUICultures"]["UICulture"].AppendChild($HelpUICultureVersionNode)

        [xml] $HelpInfoContent = $xml

    }
    else
    {
        #Get old culture info
        $ExistingCultures = @{}
        foreach($Culture in $HelpInfoContent.HelpInfo.SupportedUICultures.UICulture)
        {
            $ExistingCultures.Add($Culture.UICultureName, $Culture.UICultureVersion)
        }

        #If culture exists update version, if not, add culture and version
        if(-not ($HelpCulture -in $ExistingCultures.Keys))
        {
            $ExistingCultures.Add($HelpCulture,$HelpVersion)
        }
        else
        {
            $ExistingCultures[$HelpCulture] = $HelpVersion
        }

        $cultureNames = @()
        $cultureNames += $ExistingCultures.GetEnumerator()

        #write out cultures to XML
        for($i=0;$i -lt $ExistingCultures.Count; $i++)
        {
            $HelpUICultureNode = $xml.CreateElement("UICulture",$ns)


            $HelpUICultureNameNode = $xml.CreateElement("UICultureName",$ns)
            $HelpUICultureNameNode.InnerText = $cultureNames[$i].Name
            $HelpUICultureNode.AppendChild($HelpUICultureNameNode)

            $HelpUICultureVersionNode = $xml.CreateElement("UICultureVersion",$ns)
            $HelpUICultureVersionNode.InnerText = $cultureNames[$i].Value
            $HelpUICultureNode.AppendChild($HelpUICultureVersionNode)

            $xml["HelpInfo"]["SupportedUICultures"].AppendChild($HelpUICultureNode)
        }

        [xml] $HelpInfoContent = $xml
    }

    #Commit Help
    if(!(Test-Path $OutputFullPath))
    {
        New-Item -Path $OutputFolder -ItemType File -Name $HelpInfoFileNme

    }

    $HelpInfoContent.Save((Get-ChildItem $OutputFullPath).FullName)

}

function GetHelpFileName
{
    param(
        [System.Management.Automation.CommandInfo]$CommandInfo
    )

    if ($CommandInfo)
    {
        if ($CommandInfo.HelpFile)
        {
            if ([System.IO.Path]::IsPathRooted($CommandInfo.HelpFile))
            {
                return (Split-Path -Leaf $CommandInfo.HelpFile)
            }
            else
            {
                return $CommandInfo.HelpFile
            }
        }

        # overwise, lets guess it
        $module = @($CommandInfo.Module) + ($CommandInfo.Module.NestedModules) |
            Where-Object {$_.ModuleType -ne 'Manifest'} |
            Where-Object {$_.ExportedCommands.Keys -contains $CommandInfo.Name}

        if (-not $module)
        {
            Write-Warning "[GetHelpFileName] Cannot find module for $($CommandInfo.Name)"
            return
        }

        if ($module.Count -gt 1)
        {
            Write-Warning "[GetHelpFileName] Found $($module.Count) modules for $($CommandInfo.Name)"
            $module = $module | Select-Object -First 1
        }

        if (Test-Path $module.Path -Type Leaf)
        {
            # for regular modules, we can deduct the filename from the module path file
            $moduleItem = Get-Item -Path $module.Path
            if ($moduleItem.Extension -eq '.psm1') {
                $fileName = $moduleItem.BaseName
            } else {
                $fileName = $moduleItem.Name
            }
        }
        else
        {
            # if it's something like Dynamic module,
            # we  guess the desired help file name based on the module name
            $fileName = $module.Name
        }

        return "$fileName-help.xml"
    }
}

function MySetContent
{
    [OutputType([System.IO.FileInfo])]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        [Parameter(Mandatory=$true)]
        [string]$value,
        [Parameter(Mandatory=$true)]
        [System.Text.Encoding]$Encoding,
        [switch]$Force
    )

    if (Test-Path $Path)
    {
        if (Test-Path $Path -PathType Container)
        {
            Write-Error "Cannot write file to $Path, directory with the same name exists."
            return
        }

        if (-not $Force)
        {
            Write-Error "Cannot write to $Path, file exists. Use -Force to overwrite."
            return
        }
    }
    else
    {
        $dir = Split-Path $Path
        if ($dir)
        {
            $Null = New-Item -Type Directory $dir -ErrorAction SilentlyContinue;
        }
    }

    Write-Verbose "Writing to $Path with encoding = $($Encoding.EncodingName)"
    # just to create a file
    Set-Content -Path $Path -Value ''
    $resolvedPath = (Get-ChildItem $Path).FullName
    [System.IO.File]::WriteAllText($resolvedPath, $value, $Encoding)
    return (Get-ChildItem $Path)
}

function MyGetContent
{
    [OutputType([System.String])]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        [Parameter(Mandatory=$true)]
        [System.Text.Encoding]$Encoding
    )

    if (-not(Test-Path $Path))
    {
        throw "Cannot read from $Path, file does not exist."
        return
    }
    else
    {
        if (Test-Path $Path -PathType Container)
        {
            throw "Cannot read from $Path, $Path is a directory."
            return
        }
    }

    Write-Verbose "Reading from $Path with encoding = $($Encoding.EncodingName)"
    $resolvedPath = (Get-ChildItem $Path).FullName
    return [System.IO.File]::ReadAllText($resolvedPath, $Encoding)
}

function NewModuleLandingPage
{
    Param(
        [Parameter(mandatory=$true)]
        [string]
        $Path,
        [Parameter(mandatory=$true)]
        [string]
        $ModuleName,
        [Parameter(mandatory=$true,ParameterSetName="NewLandingPage")]
        [string]
        $ModuleGuid,
        [Parameter(mandatory=$true,ParameterSetName="NewLandingPage")]
        [string[]]
        $CmdletNames,
        [Parameter(mandatory=$true,ParameterSetName="NewLandingPage")]
        [string]
        $Locale,
        [Parameter(mandatory=$true,ParameterSetName="NewLandingPage")]
        [string]
        $Version,
        [Parameter(mandatory=$true,ParameterSetName="NewLandingPage")]
        [string]
        $FwLink,
        [Parameter(ParameterSetName="UpdateLandingPage")]
        [switch]
        $RefreshModulePage,
        [Parameter(mandatory=$true,ParameterSetName="UpdateLandingPage")]
        [System.Collections.Generic.List[Markdown.MAML.Model.MAML.MamlCommand]]
        $Module,
        [Parameter(mandatory=$true)]
        [System.Text.Encoding]$Encoding = $script:UTF8_NO_BOM,
        [switch]$Force
    )

    begin
    {
        $LandingPageName = $ModuleName + ".md"
        $LandingPagePath = Join-Path $Path $LandingPageName
    }

    process
    {
        $Description = "{{Manually Enter Description Here}}"

        if($RefreshModulePage)
        {
            if(Test-Path $LandingPagePath)
            {
                $OldLandingPageContent = Get-Content -Raw $LandingPagePath
                $OldMetaData = Get-MarkdownMetadata -Markdown $OldLandingPageContent
                $ModuleGuid = $OldMetaData["Module Guid"]
                $FwLink = $OldMetaData["Download Help Link"]
                $Version = $OldMetaData["Help Version"]
                $Locale = $OldMetaData["Locale"]

                $p = NewMarkdownParser
                $model = $p.ParseString($OldLandingPageContent)
                $index = $model.Children.IndexOf(($model.Children | Where-Object {$_.Text -eq "Description"}))
                $i = 1
                $stillParagraph = $true
                $Description = ""
                while($stillParagraph -eq $true)
                {
                    $Description += $model.Children[$index + $i].spans.text
                    $i++

                    if($model.Children[$i].NodeType -eq "Heading")
                    {
                        $stillParagraph = $false
                    }
                }
            }
            else
            {
                $ModuleGuid = "{{ Update Module Guid }}"
                $FwLink = "{{ Update Download Link }}"
                $Version = "{{ Update Help Version }}"
                $Locale = "{{ Update Locale }}"
                $Description = "{{Manually Enter Description Here}}"
            }
        }

        $Content = "---`r`nModule Name: $ModuleName`r`nModule Guid: $ModuleGuid`r`nDownload Help Link: $FwLink`r`n"
        $Content += "Help Version: $Version`r`nLocale: $Locale`r`n"
        $Content += "---`r`n`r`n"
        $Content += "# $ModuleName Module`r`n## Description`r`n"
        $Content += "$Description`r`n`r`n## $ModuleName Cmdlets`r`n"

        if($RefreshModulePage)
        {
            $Module | ForEach-Object {
                $command = $_
                if(-not $command.Synopsis)
                {
                    $Content += "### [" + $command.Name + "](" + $command.Name + ".md)`r`n{{Manually Enter " + $command.Name + " Description Here}}`r`n`r`n"
                }
                else
                {
                    $Content += "### [" + $command.Name + "](" + $command.Name + ".md)`r`n" + $command.Synopsis + "`r`n`r`n"
                }
            }
        }
        else
        {
            $CmdletNames | ForEach-Object {
                $Content += "### [" + $_ + "](" + $_ + ".md)`r`n{{Manually Enter $_ Description Here}}`r`n`r`n"
            }
        }

        MySetContent -Path $LandingPagePath -value $Content -Encoding $Encoding -Force:$Force # yield
    }

}

function ConvertMamlModelToMarkdown
{
    param(
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        [Markdown.MAML.Model.MAML.MamlCommand]$mamlCommand,

        [hashtable]$metadata,

        [switch]$NoMetadata,

        [switch]$PreserveFormatting
    )

    begin
    {
        $pipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMarkdown().Configure({
            param ($config)

            if ($NoMetadata) {
                $config.UseNoMetadata();
            }

            if ($PreserveFormatting) {
                $config.UsePreserveFormatting();
            }

            $config.SetOnlineVersionUrl();
        }).Build();
    }

    process
    {
        if ($Null -ne $metadata)
        {
            $mamlCommand.SetMetadata($metadata);
        }

        return $pipeline.Process($mamlCommand);
    }
}

function ConvertMamlModelToMarkdown2
{
    param(
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory=$true)]
        [Markdown.MAML.Model.MAML.MamlCommand]$mamlCommand,
        
        [hashtable]$metadata,

        [switch]$NoMetadata,
        
        [switch]$PreserveFormatting,

        [switch]$AlphabeticParamsOrder
    )

    begin
    {
        $pipeline = [Markdown.MAML.Pipeline.PipelineBuilder]::ToMarkdown().Configure({
            param ($config)

            $config.UseFirstExample();

            if ($NoMetadata) {
                $config.UseNoMetadata();
            }

            if ($PreserveFormatting) {
                $config.UsePreserveFormatting();
            }

            if ($AlphabeticParamsOrder) {
                $config.UseSortParamsAlphabetic();
            }

            $config.SetOnlineVersionUrl();
        }).Build();
    }

    process
    {
        if ($Null -ne $metadata)
        {
            $mamlCommand.SetMetadata($metadata);
        }

        return $pipeline.Process($mamlCommand);
    }
}

function GetCommands
{
    param(
        [Parameter(Mandatory=$true)]
        [string]$Module,
        # return names, instead of objects
        [switch]$AsNames,
        # use Session for remoting support
        [System.Management.Automation.Runspaces.PSSession]$Session
    )

    process {
        # Get-Module doesn't know about Microsoft.PowerShell.Core, so we don't use (Get-Module).ExportedCommands

        # We use: & (dummy module) {...} syntax to workaround
        # the case `GetMamlObject -Module platyPS`
        # because in this case, we are in the module context and Get-Command returns all commands,
        # not only exported ones.
        $commands = & (New-Module {}) ([scriptblock]::Create("Get-Command -Module '$Module'")) |
            Where-Object {$_.CommandType -ne 'Alias'}  # we don't want aliases in the markdown output for a module

        if ($AsNames)
        {
            $commands.Name
        }
        else
        {
            if ($Session) {
                $commands.Name | ForEach-Object {
                    # yeild
                    MyGetCommand -Cmdlet $_ -Session $Session
                }
            } else {
                $commands
            }
        }
    }
}

<#
    Get a compact string representation from TypeInfo or TypeInfo-like object

    The typeObjectHash api is provided for the remoting support.
    We use two different parameter sets ensure the tupe of -TypeObject
#>
function GetTypeString
{
    param(
        [Parameter(ValueFromPipeline=$true, ParameterSetName='typeObject')]
        [System.Reflection.TypeInfo]
        $TypeObject,

        [Parameter(ValueFromPipeline=$true, ParameterSetName='typeObjectHash')]
        [PsObject]
        $TypeObjectHash
    )

    if ($TypeObject) {
        $TypeObjectHash = $TypeObject
    }

    # special case for nullable value types
    if ($TypeObjectHash.Name -eq 'Nullable`1')
    {
        return $TypeObjectHash.GenericTypeArguments.Name
    }

    if ($TypeObjectHash.IsGenericType)
    {
        # keep information about generic parameters
        return $TypeObjectHash.ToString()
    }

    return $TypeObjectHash.Name
}

<#
    This function proxies Get-Command call.

    In case of the Remote module, we need to jump thru some hoops
    to get the actual Command object with proper fields.
    Remoting doesn't properly serialize command objects, so we need to be creative
    while extracting all the required metadata from the remote session
    See https://github.com/PowerShell/platyPS/issues/338 for historical context.
#>
function MyGetCommand
{
    Param(
        [CmdletBinding()]
        [parameter(mandatory=$true, parametersetname="Cmdlet")]
        [string] $Cmdlet,
        [System.Management.Automation.Runspaces.PSSession]$Session
    )
    # if there is no remoting, just proxy to Get-Command
    if (-not $Session) {
        return Get-Command $Cmdlet
    }

    # Here is the structure that we use in ConvertPsObjectsToMamlModel
    # we fill it up from the remote with some workarounds
    #
    # $Command.CommandType
    # $Command.Name
    # $Command.ModuleName
    # $Command.DefaultParameterSet
    # $Command.CmdletBinding
    # $ParameterSet in $Command.ParameterSets
    #     $ParameterSet.Name
    #     $ParameterSet.IsDefault
    #     $Parameter in $ParameterSet.Parameters
    #         $Parameter.Name
    #         $Parameter.IsMandatory
    #         $Parameter.Aliases
    #         $Parameter.HelpMessage
    #         $Parameter.Type
    #         $Parameter.ParameterType
    #            $Parameter.ParameterType.Name
    #            $Parameter.ParameterType.GenericTypeArguments.Name
    #            $Parameter.ParameterType.IsGenericType
    #            $Parameter.ParameterType.ToString() - we get that for free from expand

    # expand first layer of properties
    function expand([string]$property) {
        Invoke-Command -Session $Session -ScriptBlock {
            Get-Command $using:Cmdlet |
            Select-Object -ExpandProperty $using:property
        }
    }

    # This Select-Object -Skip | Select-Object -SkipLast
    # looks a little crazy, but this is just a workaround for
    # https://github.com/PowerShell/PowerShell/issues/6979
    # -First and -Index breaks the subsequent Get-Help calls

    # expand second layer of properties on the selected item
    function expand2([string]$property1, [int]$num, [int]$totalNum, [string]$property2) {
        $skipLast = $totalNum - $num - 1
        Invoke-Command -Session $Session -ScriptBlock {
            Get-Command $using:Cmdlet |
            Select-Object -ExpandProperty $using:property1 |
            Select-Object -Skip $using:num |
            Select-Object -SkipLast $using:skipLast |
            Select-Object -ExpandProperty $using:property2
        }
    }

    # expand second and 3rd layer of properties on the selected item
    function expand3(
        [string]$property1,
        [int]$num,
        [int]$totalNum,
        [string]$property2,
        [string]$property3
        ) {
        $skipLast = $totalNum - $num - 1
        Invoke-Command -Session $Session -ScriptBlock {
            Get-Command $using:Cmdlet |
            Select-Object -ExpandProperty $using:property1 |
            Select-Object -Skip $using:num |
            Select-Object -SkipLast $using:skipLast |
            Select-Object -ExpandProperty $using:property2 |
            Select-Object -ExpandProperty $using:property3
        }
    }

    function local([string]$property) {
        Get-Command $Cmdlet | select-object -ExpandProperty $property
    }

    # helper function to fill up the parameters metadata
    function getParams([int]$num, [int]$totalNum) {
        # this call we need to fill-up ParameterSets.Parameters.ParameterType with metadata
        $parameterType = expand3 'ParameterSets' $num $totalNum 'Parameters' 'ParameterType'
        # this call we need to fill-up ParameterSets.Parameters with metadata
        $parameters = expand2 'ParameterSets' $num $totalNum 'Parameters'
        if ($parameters.Length -ne $parameterType.Length) {
            $errStr = "Metadata for $Cmdlet doesn't match length.`n" +
            "This should never happen! Please report the issue on https://github.com/PowerShell/platyPS/issues"
            Write-Error $errStr
        }

        foreach ($i in 0..($parameters.Length - 1)) {
            $typeObjectHash = New-Object -TypeName pscustomobject -Property @{
                Name = $parameterType[$i].Name
                IsGenericType = $parameterType[$i].IsGenericType
                # almost .ParameterType.GenericTypeArguments.Name
                # TODO: doesn't it worth another round-trip to make it more accurate
                # and query for the Name?
                GenericTypeArguments = @{ Name = $parameterType[$i].GenericTypeArguments }
            }
            Add-Member -Type NoteProperty -InputObject $parameters[$i] -Name 'ParameterTypeName' -Value (GetTypeString -TypeObjectHash $typeObjectHash)
        }
        return $parameters
    }

    # we cannot use the nested properties from this $remote command.
    # ps remoting doesn't serialize all of them properly.
    # but we can use the top-level onces
    $remote = Invoke-Command -Session $Session { Get-Command $using:Cmdlet }

    $psets = expand 'ParameterSets'
    $psetsArray = @()
    foreach ($i in 0..($psets.Count - 1)) {
        $parameters = getParams $i $psets.Count
        $psetsArray += @(New-Object -TypeName pscustomobject -Property @{
            Name = $psets[$i].Name
            IsDefault = $psets[$i].IsDefault
            Parameters = $parameters
        })
    }

    $commandHash = @{
        Name = $Cmdlet
        CommandType = $remote.CommandType
        DefaultParameterSet = $remote.DefaultParameterSet
        CmdletBinding = $remote.CmdletBinding
        # for office we cannot get the module name from the remote, grab the local one instead
        ModuleName = local 'ModuleName'
        ParameterSets = $psetsArray
    }

    return New-Object -TypeName pscustomobject -Property $commandHash
}

function HasModule {

    param (
        [Parameter(Mandatory = $True)]
        [string]$Module
    )

    if ($Module -eq 'Microsoft.PowerShell.Core') {
        return $True;
    }

    return $Null -ne (Get-Module -Name $Module);
}

<#
    This function prepares help and command object (possibly do mock)
    and passes it to ConvertPsObjectsToMamlModel, then return results
#>
function GetMamlObject
{
    Param(
        [CmdletBinding()]
        [parameter(mandatory=$true, parametersetname="Cmdlet")]
        [string]$Cmdlet,
        [parameter(mandatory=$true, parametersetname="Module")]
        [string]$Module,
        [parameter(mandatory=$true, parametersetname="Maml")]
        [string]$MamlFile,
        [parameter(parametersetname="Maml")]
        [switch]$ConvertNotesToList,
        [parameter(parametersetname="Maml")]
        [switch]$ConvertDoubleDashLists,
        [switch]$UseFullTypeName,
        [parameter(parametersetname="Cmdlet")]
        [parameter(parametersetname="Module")]
        [System.Management.Automation.Runspaces.PSSession]$Session
    )

    function CommandHasAutogeneratedSynopsis
    {
        param([object]$help)

        return (Get-Command $help.Name -Syntax) -eq ($help.Synopsis)
    }

    if($Cmdlet)
    {
        Write-Verbose ("Processing: " + $Cmdlet)
        $Help = Get-Help $Cmdlet
        $Command = MyGetCommand -Session $Session -Cmdlet $Cmdlet
        return ConvertPsObjectsToMamlModel -Command $Command -Help $Help -UsePlaceholderForSynopsis:(CommandHasAutogeneratedSynopsis $Help) -UseFullTypeName:$UseFullTypeName
    }
    elseif ($Module)
    {
        Write-Verbose ("Processing: " + $Module)

        # GetCommands is slow over remoting, piping here is important for good UX
        GetCommands $Module -Session $Session | ForEach-Object {
            $Command = $_
            Write-Verbose ("Processing: " + $Command.Name)
            $Help = Get-Help $Command.Name
            # yield
            ConvertPsObjectsToMamlModel -Command $Command -Help $Help -UsePlaceholderForSynopsis:(CommandHasAutogeneratedSynopsis $Help)  -UseFullTypeName:$UseFullTypeName
        }
    }
    else # Maml
    {
        $HelpCollection = Get-HelpPreview -Path $MamlFile -ConvertNotesToList:$ConvertNotesToList -ConvertDoubleDashLists:$ConvertDoubleDashLists

        #Provides Name, CommandType, and Empty Module name from MAML generated module in the $command object.
        #Otherwise loads the results from Get-Command <Cmdlet> into the $command object

        $HelpCollection | ForEach-Object {

            $Help = $_

            $Command = [PsObject] @{
                Name = $Help.Name
                CommandType = $Help.Category
                HelpFile = (Split-Path $MamlFile -Leaf)
            }

            # yield
            ConvertPsObjectsToMamlModel -Command $Command -Help $Help -UseHelpForParametersMetadata -UseFullTypeName:$UseFullTypeName
        }
    }
}

function AddLineBreaksForParagraphs
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false, ValueFromPipeline=$true)]
        [string]$text
    )

    begin
    {
        $paragraphs = @()
    }

    process
    {
        $text = $text.Trim()
        $paragraphs += $text
    }

    end
    {
        $paragraphs -join "`r`n`r`n"
    }
}

function DescriptionToPara
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false, ValueFromPipeline=$true)]
        $description
    )

    process
    {
        # on some old maml modules description uses Tag to store *-bullet-points
        # one example of it is Exchange
        $description.Tag + "" + $description.Text
    }
}

function IncrementHelpVersion
{
    param(
        [string]
        $HelpVersionString
    )
    process
    {
        if($HelpVersionString -eq "{{Please enter version of help manually (X.X.X.X) format}}")
        {
            return "1.0.0.0"
        }
        $lastDigitPosition = $HelpVersionString.LastIndexOf(".") + 1
        $frontDigits = $HelpVersionString.Substring(0,$lastDigitPosition)
        $frontDigits += ([int] $HelpVersionString.Substring($lastDigitPosition)) + 1
        return $frontDigits
    }
}

<#
    This function converts help and command object (possibly mocked) into a Maml Model
#>
function ConvertPsObjectsToMamlModel
{
    [CmdletBinding()]
    [OutputType([Markdown.MAML.Model.MAML.MamlCommand])]
    param(
        [Parameter(Mandatory=$true)]
        [object]$Command,
        [Parameter(Mandatory=$true)]
        [object]$Help,
        [switch]$UseHelpForParametersMetadata,
        [switch]$UsePlaceholderForSynopsis,
        [switch]$UseFullTypeName
    )

    function isCommonParameterName {
        param([string]$parameterName, [switch]$Workflow)

        if (@(
                'Verbose',
                'Debug',
                'ErrorAction',
                'WarningAction',
                'InformationAction',
                'ErrorVariable',
                'WarningVariable',
                'InformationVariable',
                'OutVariable',
                'OutBuffer',
                'PipelineVariable'
        ) -contains $parameterName) {
            return $true
        }

        if ($Workflow)
        {
            return @(
                'PSParameterCollection',
                'PSComputerName',
                'PSCredential',
                'PSConnectionRetryCount',
                'PSConnectionRetryIntervalSec',
                'PSRunningTimeoutSec',
                'PSElapsedTimeoutSec',
                'PSPersist',
                'PSAuthentication',
                'PSAuthenticationLevel',
                'PSApplicationName',
                'PSPort',
                'PSUseSSL',
                'PSConfigurationName',
                'PSConnectionURI',
                'PSAllowRedirection',
                'PSSessionOption',
                'PSCertificateThumbprint',
                'PSPrivateMetadata',
                'AsJob',
                'JobName'
            ) -contains $parameterName
        }

        return $false
    }

    function getPipelineValue($Parameter) {
        if ($Parameter.ValueFromPipeline)
        {
            if ($Parameter.ValueFromPipelineByPropertyName)
            {
                return 'True (ByPropertyName, ByValue)'
            }
            else
            {
                return 'True (ByValue)'
            }
        }
        else
        {
            if ($Parameter.ValueFromPipelineByPropertyName)
            {
                return 'True (ByPropertyName)'
            }
            else
            {
                return 'False'
            }
        }
    }

    function getTypeString {
        param(
            [Parameter(ValueFromPipeline=$true)]
            [System.Reflection.TypeInfo]
            $typeObject
        )
        
        # special case for nullable value types
        if ($typeObject.Name -eq 'Nullable`1')
        {
            return $typeObject.GenericTypeArguments.Name
        }

        if ($typeObject.IsGenericType)
        {
            # keep information about generic parameters
            return $typeObject.ToString()
        }

        return $typeObject.Name
    }

    function normalizeFirstLatter {
        param(
            [Parameter(ValueFromPipeline=$true)]
            [string]$value
        )

        if ($value -and $value.Length -gt 0)
        {
            return $value.Substring(0,1).ToUpperInvariant() + $value.substring(1)
        }

        return $value
    }

    #endregion

    $builder = [Markdown.MAML.Model.MAML.MamlCommandBuilder]::Create($Command.Name, $Command.ModuleName);

    #Get Synopsis
    if (!$UsePlaceholderForSynopsis)
    {
        $builder.Synopsis($Help.Synopsis);
    }

    $builder.Description($Help.description.Text);
    $builder.Notes($Help.alertSet.alert.Text);

    #Add to relatedLinks
    foreach($link in $Help.relatedLinks.navigationLink) {
        $builder.Link($link.linkText, $link.uri);
    }

    #Add Examples
    foreach($example in $Help.examples.example)
    {
        $builder.Example($example.title, $example.introduction, $example.code, $example.remarks.text);
    }

    # Get Inputs
    # Reccomend adding a Parameter Name and Parameter Set Name to each input object.
    foreach ($inputType in $Help.inputTypes.inputType) {
        $builder.Input($inputType.type.name, $inputType.description.Text);
    }
    
    # Get Outputs
    # No Output Type description is provided from the command object.
    foreach ($outputType in $Help.returnValues.returnValue) {
        $builder.Output($outputType.type.name, $outputType.description.Text);
    }

    #region Command Object Values Processing

    $IsWorkflow = $Command.CommandType -eq 'Workflow';

    #Get Syntax
    #region Get the Syntax Parameter Set objects

    function FillUpParameterFromHelp {
        param(
            [Parameter(Mandatory=$true)]
            [Markdown.MAML.Model.MAML.MamlParameter]$ParameterObject
        )

        $HelpEntry = $Help.parameters.parameter | Where-Object {$_.Name -eq $ParameterObject.Name}

        $ParameterObject.DefaultValue = $HelpEntry.defaultValue | normalizeFirstLatter
        $ParameterObject.VariableLength = $HelpEntry.variableLength -eq 'True'
        # $ParameterObject.Position = $HelpEntry.position | normalizeFirstLatter
        # $ParameterObject.Globbing = $HelpEntry.globbing -eq 'True'
        # $ParameterObject.Position = $HelpEntry.position -as [byte]
        if ($HelpEntry.description)
        {
            if ($HelpEntry.description.text)
            {
                $ParameterObject.Description = $HelpEntry.description |
                    DescriptionToPara |
                    AddLineBreaksForParagraphs
            }
            else
            {
                # this case happens, when there is HelpMessage in 'Parameter' attribute,
                # but there is no maml or comment-based help.
                # then help engine put string outside of 'text' property
                # In this case there is no DescriptionToPara call needed
                $ParameterObject.Description = $HelpEntry.description | AddLineBreaksForParagraphs
            }
        }

        $syntaxParam = $Help.syntax.syntaxItem.parameter | Where-Object {$_.Name -eq $Parameter.Name} | Select-Object -First 1;

        if ($syntaxParam)
        {
            # otherwise we could potentialy get it from Reflection but not doing it for now
            $ParameterObject.parameterValueGroup = [string[]]$syntaxParam.parameterValueGroup.parameterValue
        }
    }

    function FillUpSyntaxFromCommand {
        foreach($parameterSet in $Command.ParameterSets)
        {
            $builder.Syntax($parameterSet.Name, $parameterSet.IsDefault);

            foreach($parameter in $parameterSet.Parameters)
            {
                # ignore CommonParameters
                if (isCommonParameterName $parameter.Name -Workflow:$IsWorkflow)
                {
                    # but don't ignore them, if they have explicit help entries
                    if ($Help.parameters.parameter | Where-Object {$_.Name -eq $parameter.Name})
                    {
                    }
                    else
                    {
                        continue
                    }
                }

                $parameterType = $parameter.ParameterType;
                
                # Add a parameter
                $parameterObject = $builder.Parameter(
                    $ParameterSet.Name,

                    # Name
                    $parameter.Name,

                    # Description
                    $parameter.HelpMessage,

                    # Required
                    $parameter.IsMandatory,

                    # Type
                    (getTypeString -typeObject $ParameterType),

                    # Position
                    $parameter.Position,

                    # Aliases
                    $parameter.Aliases,

                    # Pipeline input
                    (getPipelineValue $parameter),

                    # FullType
                    $parameterType.ToString()
                );

                # Check if [SupportsWildcards()] is present
                if ($Null -ne ($Parameter.Attributes | Where-Object -FilterScript { $_ -is [System.Management.Automation.SupportsWildcardsAttribute] })) {
                    $parameterObject.Globbing = $True;
                }

                FillUpParameterFromHelp -ParameterObject $parameterObject;
            }
        }
    }

    function FillUpSyntaxFromHelp {
        $paramSetCount = 0
        foreach($parameterSet in $Help.syntax.syntaxItem)
        {
            $syntaxObject = New-Object -TypeName Markdown.MAML.Model.MAML.MamlSyntax

            $paramSetCount++
            $syntaxObject.ParameterSetName = $script:SET_NAME_PLACEHOLDER + "_" + $paramSetCount

            foreach($parameter in $parameterSet.Parameter)
            {
                $position = $Null;

                if ($Null -ne $parameter.position -and $parameter.position -ne 'named') {
                    $position = $parameter.position;
                }

                $parameterObject = $builder.Parameter(
                    $syntaxObject.ParameterSetName,

                    # Name
                    $parameter.Name,

                    # Description
                    '',

                    # Required
                    $parameter.required -eq 'true',

                    # Type
                    $parameter.parameterValue,

                    # Position
                    $position,

                    # Aliases
                    $parameter.Aliases,

                    # Pipeline input
                    ($parameter.pipelineInput | normalizeFirstLatter),

                    # FullType
                    ''
                );
                
                FillUpParameterFromHelp -ParameterObject $parameterObject;
            }
        }
    }

    if ($UseHelpForParametersMetadata)
    {
        FillUpSyntaxFromHelp
    }
    else
    {
        FillUpSyntaxFromCommand
    }

    # Get the built command object
    $MamlCommandObject = $builder.Get();

    function Get-ParameterByName
    {
        param(
            [string]$Name
        )

        $defaultSyntax = $MamlCommandObject.Syntax | Where-Object { $Command.DefaultParameterSet -eq $_.ParameterSetName }
        # default syntax should have a priority
        $syntaxes = @($defaultSyntax) + $MamlCommandObject.Syntax

        foreach ($s in $syntaxes)
        {
            $param = $s.Parameters | Where-Object { $_.Name -eq $Name }
            if ($param)
            {
                return $param
            }
        }
    }

    function Get-ParameterNamesOrder()
    {
        # we want to keep original order for existing help
        # if something changed:
        #   - remove it from it's position
        #   - add to the end

        $helpNames = $Help.parameters.parameter.Name
        if (-not $helpNames) { $helpNames = @() }

        # sort-object unique does case-insensiteve unification
        $realNames = $MamlCommandObject.Syntax.Parameters.Name | Sort-object -Unique
        if (-not $realNames) { $realNames = @() }

        $realNamesList = New-Object 'System.Collections.Generic.List[string]'
        $realNamesList.AddRange( ( [string[]] $realNames) )

        foreach ($name in $helpNames)
        {
            if ($realNamesList.Remove($name))
            {
                # yeild
                $name
            }
            # Otherwise it didn't exist
        }

        foreach ($name in $realNamesList)
        {
            # yeild
            $name
        }

    }

    foreach($ParameterName in (Get-ParameterNamesOrder))
    {
        $Parameter = Get-ParameterByName $ParameterName
        if ($Parameter)
        {
            if ($UseFullTypeName)
            {
                $Parameter = $Parameter.Clone()
                $Parameter.Type = $Parameter.FullType
            }
            $MamlCommandObject.Parameters.Add($Parameter)
        }
        else
        {
            Write-Warning "[Markdown generation] Could not find parameter object for $ParameterName in command $($Command.Name)"
        }
    }

    # Handle CommonParameters, default for MamlCommand is SupportCommonParameters = $true
    if ($Command.CmdletBinding -eq $false)
    {
        # Remove CommonParameters by exception
        $MamlCommandObject.SupportCommonParameters = $false
    }

    # Handle CommonWorkflowParameters
    $MamlCommandObject.IsWorkflow = $IsWorkflow

    return $MamlCommandObject
}

function validateWorkingProvider
{
    if((Get-Location).Drive.Provider.Name -ne 'FileSystem')
    {
        Write-Verbose 'PlatyPS Cmdlets only work in the FileSystem Provider. PlatyPS is changing the provider of this session back to filesystem.'
        $AvailableFileSystemDrives = Get-PSDrive | Where-Object {$_.Provider.Name -eq "FileSystem"} | Select-Object Root
        if($AvailableFileSystemDrives.Count -gt 0)
        {
           Set-Location $AvailableFileSystemDrives[0].Root
        }
        else
        {
             throw 'PlatyPS Cmdlets only work in the FileSystem Provider.'
        }
    }
}
#endregion

#region Parameter Auto Completers


#                                       bbbbbbbb
# TTTTTTTTTTTTTTTTTTTTTTT               b::::::b                                     CCCCCCCCCCCCC                                                             lllllll                              tttt            iiii
# T:::::::::::::::::::::T               b::::::b                                  CCC::::::::::::C                                                             l:::::l                           ttt:::t           i::::i
# T:::::::::::::::::::::T               b::::::b                                CC:::::::::::::::C                                                             l:::::l                           t:::::t            iiii
# T:::::TT:::::::TT:::::T                b:::::b                               C:::::CCCCCCCC::::C                                                             l:::::l                           t:::::t
# TTTTTT  T:::::T  TTTTTTaaaaaaaaaaaaa   b:::::bbbbbbbbb                      C:::::C       CCCCCC   ooooooooooo      mmmmmmm    mmmmmmm   ppppp   ppppppppp    l::::l     eeeeeeeeeeee    ttttttt:::::ttttttt    iiiiiii    ooooooooooo   nnnn  nnnnnnnn
#         T:::::T        a::::::::::::a  b::::::::::::::bb                   C:::::C               oo:::::::::::oo  mm:::::::m  m:::::::mm p::::ppp:::::::::p   l::::l   ee::::::::::::ee  t:::::::::::::::::t    i:::::i  oo:::::::::::oo n:::nn::::::::nn
#         T:::::T        aaaaaaaaa:::::a b::::::::::::::::b                  C:::::C              o:::::::::::::::om::::::::::mm::::::::::mp:::::::::::::::::p  l::::l  e::::::eeeee:::::eet:::::::::::::::::t     i::::i o:::::::::::::::on::::::::::::::nn
#         T:::::T                 a::::a b:::::bbbbb:::::::b --------------- C:::::C              o:::::ooooo:::::om::::::::::::::::::::::mpp::::::ppppp::::::p l::::l e::::::e     e:::::etttttt:::::::tttttt     i::::i o:::::ooooo:::::onn:::::::::::::::n
#         T:::::T          aaaaaaa:::::a b:::::b    b::::::b -:::::::::::::- C:::::C              o::::o     o::::om:::::mmm::::::mmm:::::m p:::::p     p:::::p l::::l e:::::::eeeee::::::e      t:::::t           i::::i o::::o     o::::o  n:::::nnnn:::::n
#         T:::::T        aa::::::::::::a b:::::b     b:::::b --------------- C:::::C              o::::o     o::::om::::m   m::::m   m::::m p:::::p     p:::::p l::::l e:::::::::::::::::e       t:::::t           i::::i o::::o     o::::o  n::::n    n::::n
#         T:::::T       a::::aaaa::::::a b:::::b     b:::::b                 C:::::C              o::::o     o::::om::::m   m::::m   m::::m p:::::p     p:::::p l::::l e::::::eeeeeeeeeee        t:::::t           i::::i o::::o     o::::o  n::::n    n::::n
#         T:::::T      a::::a    a:::::a b:::::b     b:::::b                  C:::::C       CCCCCCo::::o     o::::om::::m   m::::m   m::::m p:::::p    p::::::p l::::l e:::::::e                 t:::::t    tttttt i::::i o::::o     o::::o  n::::n    n::::n
#       TT:::::::TT    a::::a    a:::::a b:::::bbbbbb::::::b                   C:::::CCCCCCCC::::Co:::::ooooo:::::om::::m   m::::m   m::::m p:::::ppppp:::::::pl::::::le::::::::e                t::::::tttt:::::ti::::::io:::::ooooo:::::o  n::::n    n::::n
#       T:::::::::T    a:::::aaaa::::::a b::::::::::::::::b                     CC:::::::::::::::Co:::::::::::::::om::::m   m::::m   m::::m p::::::::::::::::p l::::::l e::::::::eeeeeeee        tt::::::::::::::ti::::::io:::::::::::::::o  n::::n    n::::n
#       T:::::::::T     a::::::::::aa:::ab:::::::::::::::b                        CCC::::::::::::C oo:::::::::::oo m::::m   m::::m   m::::m p::::::::::::::pp  l::::::l  ee:::::::::::::e          tt:::::::::::tti::::::i oo:::::::::::oo   n::::n    n::::n
#       TTTTTTTTTTT      aaaaaaaaaa  aaaabbbbbbbbbbbbbbbb                            CCCCCCCCCCCCC   ooooooooooo   mmmmmm   mmmmmm   mmmmmm p::::::pppppppp    llllllll    eeeeeeeeeeeeee            ttttttttttt  iiiiiiii   ooooooooooo     nnnnnn    nnnnnn
#                                                                                                                                           p:::::p
#                                                                                                                                           p:::::p
#                                                                                                                                          p:::::::p
#                                                                                                                                          p:::::::p
#                                                                                                                                          p:::::::p
#                                                                                                                                          ppppppppp


# Register-ArgumentCompleter can be provided thru TabExpansionPlusPlus or with V5 inbox module.
# We don't care much which one it is, but the inbox one doesn't have -Description parameter
if (Get-Command -Name Register-ArgumentCompleter -Module TabExpansionPlusPlus -ErrorAction Ignore) {
    Function ModuleNameCompleter {
        Param (
            $commandName,
            $parameterName,
            $wordToComplete,
            $commandAst,
            $fakeBoundParameter
        )

        Get-Module -Name "$wordToComplete*" |
            ForEach-Object {
                New-CompletionResult -CompletionText $_.Name -ToolTip $_.Description
            }
    }

    Register-ArgumentCompleter -CommandName New-MarkdownHelp -ParameterName Module -ScriptBlock $Function:ModuleNameCompleter -Description 'This argument completer handles the -Module parameter of the New-MarkdownHelp Command.'
}
elseif (Get-Command -Name Register-ArgumentCompleter -ErrorAction Ignore) {
    Function ModuleNameCompleter {
        Param (
            $commandName,
            $parameterName,
            $wordToComplete,
            $commandAst,
            $fakeBoundParameter
        )

        Get-Module -Name "$wordToComplete*" |
            ForEach-Object {
                $_.Name
            }
    }

    Register-ArgumentCompleter -CommandName New-MarkdownHelp -ParameterName Module -ScriptBlock $Function:ModuleNameCompleter
}

#endregion Parameter Auto Completers
