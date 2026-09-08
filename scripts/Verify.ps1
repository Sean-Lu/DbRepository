#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $BaseRef = 'HEAD',
    [string] $MSBuildPath
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root ('.artifacts/verify/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
Write-Host "验证产物：$output"

function Invoke-Checked([string] $File, [string[]] $Arguments, [string] $Log) {
    & $File @Arguments *> $Log
    if ($LASTEXITCODE -ne 0) {
        Get-Content $Log -Tail 40 | Write-Host
        throw "命令失败（退出码 $LASTEXITCODE），完整日志：$Log"
    }
}

function Get-CompilerWarnings([string] $Log, [string] $SourceRoot) {
    # 不比较行号，但保留文件、目标框架、诊断内容与次数，避免代码移动误报或重复警告漏报。
    $counts = @{}
    foreach ($line in Get-Content $Log) {
        if ($line -match ': warning ((?!NU190[0-5]\b)[A-Z]+\d+):') {
            $key = $line.Replace($SourceRoot, '<root>') -replace '\(\d+(,\d+)*\)', '(line)'
            $key = $key.Trim()
            $counts[$key] = 1 + $counts[$key]
        }
    }
    return $counts
}

$oldLanguage = $env:VSLANG
$oldDotnetLanguage = $env:DOTNET_CLI_UI_LANGUAGE
Push-Location $root
try {
    $env:VSLANG = '1033'
    $env:DOTNET_CLI_UI_LANGUAGE = 'en-US'
    if (!$MSBuildPath) {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
        $MSBuildPath = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
    }
    if (!$MSBuildPath -or !(Test-Path -LiteralPath $MSBuildPath)) { throw '需要安装 Visual Studio MSBuild 和 .NET Framework 目标包。' }

    # 在独立目录导出基线，不切换工作区、不暂存文件；两次编译使用相同工具链。
    $commit = & git rev-parse --verify "$BaseRef^{commit}"
    if ($LASTEXITCODE -ne 0) { throw "无效基线：$BaseRef" }
    $baseline = Join-Path $output 'baseline'
    New-Item -ItemType Directory -Path $baseline | Out-Null
    $archive = Join-Path $output 'baseline.tar'
    Invoke-Checked git @('archive', '--format=tar', "--output=$archive", $commit) (Join-Path $output 'archive.log')
    Invoke-Checked tar @('-xf', $archive, '-C', $baseline) (Join-Path $output 'extract.log')
    $config = Join-Path $root 'config/ci.NuGet.Config'
    $common = @('/p:Configuration=Release', '/p:GeneratePackageOnBuild=false', '/m', '/v:minimal', '/nologo')
    $restore = @('/t:Restore', "/p:RestoreConfigFile=$config", '/p:RestoreForce=true')
    Invoke-Checked $MSBuildPath (@((Join-Path $baseline 'DbRepository.sln')) + $common + $restore + '/p:NuGetAudit=false') (Join-Path $output 'baseline-restore.log')
    Invoke-Checked $MSBuildPath (@((Join-Path $baseline 'DbRepository.sln')) + $common + '/t:Rebuild') (Join-Path $output 'baseline-build.log')

    # 当前依赖包含传递依赖；漏洞或审计源不可用均阻止通过，不自动更新任何包。
    $auditLog = Join-Path $output 'restore-audit.log'
    Invoke-Checked $MSBuildPath (@('DbRepository.sln') + $common + $restore + @('/p:NuGetAudit=true', '/p:NuGetAuditMode=all', '/p:NuGetAuditLevel=low')) $auditLog
    $failures = @()
    if (Select-String -Path $auditLog -Pattern '\bNU190[0-5]\b' -Quiet) { $failures += "依赖审计未通过：$auditLog" }
    $buildLog = Join-Path $output 'build.log'
    Invoke-Checked $MSBuildPath (@('DbRepository.sln') + $common + '/t:Rebuild') $buildLog

    $before = Get-CompilerWarnings (Join-Path $output 'baseline-build.log') $baseline
    $after = Get-CompilerWarnings $buildLog $root
    $added = @($after.Keys | Where-Object { $after[$_] -gt $before[$_] } | Sort-Object)
    $added | Set-Content (Join-Path $output 'new-warnings.log')
    if ($added.Count) { $failures += "新增编译或 MSTest 警告：$($added.Count) 项，详见 new-warnings.log。" }

    Invoke-Checked dotnet @('test', 'test/Sean.Core.DbRepository.Test/Sean.Core.DbRepository.Test.csproj', '-c', 'Release', '--no-build', '--no-restore', '--collect:XPlat Code Coverage', '--logger:trx', '--results-directory', (Join-Path $output 'tests')) (Join-Path $output 'test.log')
    [xml] $testReport = Get-Content (Get-ChildItem (Join-Path $output 'tests') -Filter '*.trx').FullName -Raw
    $counters = $testReport.TestRun.ResultSummary.Counters
    if ([int] $counters.total -eq 0 -or [int] $counters.passed -ne [int] $counters.total) { throw '测试未全部执行通过。' }
    # 只取收集器的原始附件，避免将 TRX 在 In 目录复制的同一报告重复计数。
    $coverage = @(Get-ChildItem (Join-Path $output 'tests') -Filter coverage.cobertura.xml -Depth 1)
    if ($coverage.Count -ne 1) { throw '未生成唯一的覆盖率报告。' }
    [xml] $report = Get-Content $coverage[0].FullName -Raw
    "行覆盖率：$($report.coverage.'line-rate')；分支覆盖率：$($report.coverage.'branch-rate')" | Tee-Object -FilePath (Join-Path $output 'coverage-summary.log') | Write-Host

    foreach ($project in 'Sean.Core.DbRepository', 'Sean.Core.DbRepository.Dapper') {
        Invoke-Checked dotnet @('pack', "src/$project/$project.csproj", '-c', 'Release', '--no-build', '--no-restore', '-p:GeneratePackageOnBuild=false', '-o', (Join-Path $output 'packages')) (Join-Path $output "pack-$project.log")
    }
    if (@(Get-ChildItem (Join-Path $output 'packages') -Filter '*.nupkg').Count -ne 2) { throw '预期生成两个 ORM 包。' }
    # 审计和警告失败不跳过测试、打包，但最终退出码仍必须失败。
    if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
    Write-Host '验证通过；未暂存、提交或发布。覆盖率变化仍需结合当前批次人工审阅。'
}
finally {
    Pop-Location
    $env:VSLANG = $oldLanguage
    $env:DOTNET_CLI_UI_LANGUAGE = $oldDotnetLanguage
}
