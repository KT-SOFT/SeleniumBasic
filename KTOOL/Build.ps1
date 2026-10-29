param([switch]$Test)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskTools=Join-Path $PSScriptRoot 'tools'
$taskDist=Join-Path $PSScriptRoot 'dist'
$null=New-Item -ItemType Directory -Path $taskTools,$taskDist -Force
[Net.ServicePointManager]::SecurityProtocol=[Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
function Get-Checked([string]$uri,[string]$file,[string]$sha){
 if(-not(Test-Path -LiteralPath $file) -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $sha){
  $part=$file+'.download';Invoke-WebRequest -Uri $uri -OutFile $part -UseBasicParsing
  if((Get-FileHash -LiteralPath $part -Algorithm SHA256).Hash -ne $sha){throw ('Download verification failed: '+$uri)}
  Copy-Item -LiteralPath $part -Destination $file -Force;Remove-Item -LiteralPath $part
 }
 return $file
}
$taskExtractorZip=Get-Checked 'https://constexpr.org/innoextract/files/innoextract-1.9-windows.zip' (Join-Path $taskTools 'innoextract-1.9-windows.zip') '6989342C9B026A00A72A38F23B62A8E6A22CC5DE69805CF47D68AC2FEC993065'
$taskExtractFolder=Join-Path $taskTools 'innoextract'
if(-not(Test-Path -LiteralPath $taskExtractFolder)){Expand-Archive -LiteralPath $taskExtractorZip -DestinationPath $taskExtractFolder}
$taskExtractor=(Get-ChildItem -LiteralPath $taskExtractFolder -Recurse -File -Filter 'innoextract.exe'|Select-Object -First 1).FullName
$taskCompilerPackage=Get-Checked 'https://github.com/jrsoftware/issrc/releases/download/is-6_0_5/innosetup-6.0.5.exe' (Join-Path $taskTools 'innosetup-6.0.5.exe') 'AE6823B523DF87E9441789E51845434E4E0E70AAC0B88AFE80F94F20F4B98ACB'
$taskCompiler=Join-Path $taskTools 'compiler\app\ISCC.exe'
if(-not(Test-Path -LiteralPath $taskCompiler)){& $taskExtractor --silent --output-dir (Join-Path $taskTools 'compiler') $taskCompilerPackage;if($LASTEXITCODE -ne 0){throw 'Compiler extraction failed'}}
$taskOriginal=Get-Checked 'https://github.com/florentbr/SeleniumBasic/releases/download/v2.0.9.0/SeleniumBasic-2.0.9.0.exe' (Join-Path $taskTools 'SeleniumBasic-2.0.9.0.exe') '0020A7BBD6D68635CD564DE4B49D38418CA228A2CE3B2E89DF0C7DB116748DDB'
$taskPayload=Join-Path $PSScriptRoot 'upstream-payload\app'
if(-not(Test-Path -LiteralPath $taskPayload)){& $taskExtractor --silent --output-dir (Split-Path -Parent $taskPayload) $taskOriginal;if($LASTEXITCODE -ne 0){throw 'Original payload extraction failed'}}
$taskReferencePackage=Get-Checked 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net48/1.0.3/microsoft.netframework.referenceassemblies.net48.1.0.3.nupkg' (Join-Path $taskTools 'microsoft.netframework.referenceassemblies.net48.1.0.3.nupkg') '8A7E348538E7EB91351696911689F49E3D4F63F8BAB517432BBE159B8B1104A2'
$taskReferenceRoot=Join-Path $taskTools 'net48-reference'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if(-not(Test-Path -LiteralPath $taskReferenceRoot)){[IO.Compression.ZipFile]::ExtractToDirectory($taskReferencePackage,$taskReferenceRoot)}
$taskFramework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if(-not(Test-Path -LiteralPath $taskFramework)){$taskFramework=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'}
$taskMsbuild=Join-Path $taskFramework 'MSBuild.exe';$taskCsc=Join-Path $taskFramework 'csc.exe'
$taskRef=Join-Path $taskReferenceRoot 'build'
& $taskMsbuild (Join-Path $taskRoot 'Selenium\Selenium.csproj') /t:Rebuild /nologo /v:quiet '/p:Configuration=Release;TargetFrameworkVersion=v4.8;RegisterForComInterop=false;SignAssembly=true;AssemblyOriginatorKeyFile=key.snk;PostBuildEvent=' ('/p:TargetFrameworkRootPath='+$taskRef) ('/p:FrameworkPathOverride='+(Join-Path $taskRef '.NETFramework\v4.8'))
if($LASTEXITCODE -ne 0){throw 'Selenium compilation failed'}
$taskBin=Join-Path $taskRoot 'Selenium\bin\Release'
$taskExport=Join-Path $taskTools 'ExportTypeLibrary.exe'
& $taskCsc /nologo /target:exe ('/out:'+$taskExport) (Join-Path $PSScriptRoot 'Installer\ExportTypeLibrary.cs')
if($LASTEXITCODE -ne 0){throw 'Type-library tool compilation failed'}
foreach($taskBits in @('32','64')){& $taskExport (Join-Path $taskBin 'Selenium.dll') (Join-Path $taskBin ('Selenium'+$taskBits+'.tlb')) $taskBits;if($LASTEXITCODE -ne 0){throw 'Type-library generation failed'}}
& $taskCsc /nologo /target:exe /platform:anycpu ('/out:'+(Join-Path $taskBin 'SeleniumBasic.DriverSetup.exe')) /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'Installer\DriverSetup.cs')
if($LASTEXITCODE -ne 0){throw 'Driver setup compilation failed'}
foreach($taskPair in @(@('Selenium.chm','Selenium\bin\Help'),@('vbsc.exe','VbsConsole\bin\Release'),@('extensions.xpi','FirefoxAddons\bin'))){$taskFolder=Join-Path $taskRoot $taskPair[1];$null=New-Item -ItemType Directory -Path $taskFolder -Force;Copy-Item -LiteralPath (Join-Path $taskPayload $taskPair[0]) -Destination (Join-Path $taskFolder $taskPair[0]) -Force}
$taskRefs=Join-Path $taskRoot 'References';$null=New-Item -ItemType Directory -Path $taskRefs -Force
foreach($taskName in @('firefoxdriver.xpi','operadriver.exe','phantomjs.exe','iedriver.exe')){Copy-Item -LiteralPath (Join-Path $taskPayload $taskName) -Destination (Join-Path $taskRefs $taskName) -Force}
foreach($taskFolder in @('Templates\Xlbin','Examples\Excel\Xlbin')){$null=New-Item -ItemType Directory -Path (Join-Path $taskRoot $taskFolder) -Force;Get-ChildItem -LiteralPath (Join-Path $taskPayload ($taskFolder -replace '\\Xlbin$','')) -File|Where-Object{$_.Extension -in @('.xlt','.xls')}|Copy-Item -Destination (Join-Path $taskRoot $taskFolder) -Force}
if($Test){
 $taskFailureFixture=Join-Path $PSScriptRoot 'staging\driver-failure'
 $null=New-Item -ItemType Directory -Path $taskFailureFixture -Force
 Copy-Item -LiteralPath (Join-Path $taskBin 'Selenium.dll') -Destination $taskFailureFixture -Force
 $taskFailureHelper=Join-Path $taskFailureFixture 'FailureHelper.cs'
 [IO.File]::WriteAllText($taskFailureHelper,'using System; class Helper { static int Main(){Console.Error.WriteLine("KTOOL_TEST_PREPARATION_FAILURE");return 1;} }')
 & $taskCsc /nologo /target:exe ('/out:'+(Join-Path $taskFailureFixture 'SeleniumBasic.DriverSetup.exe')) $taskFailureHelper
 if($LASTEXITCODE -ne 0){throw 'Failure helper compilation failed'}
 foreach($taskPlatform in @('anycpu','x86')){
  $taskTestExe=Join-Path $taskBin ('WindowRecoveryTests-'+$taskPlatform+'.exe')
  & $taskCsc /nologo /target:exe ('/platform:'+$taskPlatform) ('/out:'+$taskTestExe) ('/reference:'+(Join-Path $taskBin 'Selenium.dll')) /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'Tests\WindowRecoveryTests.cs')
  if($LASTEXITCODE -ne 0){throw 'Regression test compilation failed'}
  & $taskTestExe
  if($LASTEXITCODE -ne 0){throw ('Regression tests failed: '+$taskPlatform)}
  $taskFailureExe=Join-Path $taskFailureFixture ('DriverFailureTest-'+$taskPlatform+'.exe')
  & $taskCsc /nologo /target:exe ('/platform:'+$taskPlatform) ('/out:'+$taskFailureExe) ('/reference:'+(Join-Path $taskFailureFixture 'Selenium.dll')) /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'Tests\DriverFailureTest.cs')
  if($LASTEXITCODE -ne 0){throw 'Driver failure test compilation failed'}
  & $taskFailureExe
  if($LASTEXITCODE -ne 0){throw ('Driver failure tests failed: '+$taskPlatform)}
 }
}
& $taskCompiler /Q ('/O'+$taskDist) (Join-Path $taskRoot 'SeleniumBasicSetup.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
$taskCore=Join-Path $taskDist 'SeleniumBasic-Core-2.0.9.1.exe'
$taskFinal=Join-Path $taskDist 'SeleniumBasic-KTOOL-2.0.9.1.exe'
& $taskCsc /nologo /target:winexe /platform:anycpu ('/resource:'+$taskCore+',KTOOL.CoreInstaller') ('/out:'+$taskFinal) (Join-Path $PSScriptRoot 'Installer\SetupBootstrap.cs')
if($LASTEXITCODE -ne 0){throw 'Automatic setup bootstrap compilation failed'}
$taskHash=(Get-FileHash -LiteralPath $taskFinal -Algorithm SHA256).Hash
[IO.File]::WriteAllText((Join-Path $taskDist 'SHA256SUMS.txt'),($taskHash+'  '+[IO.Path]::GetFileName($taskFinal)+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
[pscustomobject]@{File=$taskFinal;Version=(Get-Item -LiteralPath $taskFinal).VersionInfo.FileVersion;SHA256=$taskHash;Size=(Get-Item -LiteralPath $taskFinal).Length}|ConvertTo-Json
