#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef ModStageDir
  #error ModStageDir must be defined via /DModStageDir=...
#endif
#ifndef BepInExStageDir
  #error BepInExStageDir must be defined via /DBepInExStageDir=...
#endif
#ifndef OutputDirPath
  #define OutputDirPath "..\dist\installer"
#endif

[Setup]
AppId={{B4E7F1C2-8F3E-4C4B-9D8A-6A1E7F5B2C90}
AppName=SiNiSistar2 HeatSeekar
AppVersion={#MyAppVersion}
AppVerName=SiNiSistar2 HeatSeekar {#MyAppVersion}
AppPublisher=AkiSoft
VersionInfoCompany=AkiSoft
AppCopyright=Copyright (C) AkiSoft
AppPublisherURL=https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/
AppSupportURL=https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues
AppUpdatesURL=https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest
DefaultDirName={code:GetDefaultGameDir}
UsePreviousAppDir=yes
AppendDefaultDirName=no
DirExistsWarning=no
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallFilesDir={app}\BepInEx\plugins\HeatSeekar
UninstallDisplayName=SiNiSistar2 HeatSeekar
OutputDir={#OutputDirPath}
OutputBaseFilename=SiNiSistar2.HeatSeekar-{#MyAppVersion}-win-x64-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no
WizardImageFile=assets\wizard-banner.bmp
WizardSmallImageFile=assets\wizard-small.bmp
WizardImageStretch=yes
ShowLanguageDialog=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"; LicenseFile: "NOTICE.en.txt"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"; LicenseFile: "NOTICE.ja.txt"
Name: "zhCN"; MessagesFile: "Languages\ChineseSimplified.isl"; LicenseFile: "NOTICE.zh-CN.txt"
Name: "zhTW"; MessagesFile: "Languages\ChineseTraditional.isl"; LicenseFile: "NOTICE.zh-TW.txt"
Name: "ko"; MessagesFile: "compiler:Languages\Korean.isl"; LicenseFile: "NOTICE.ko.txt"

[Messages]
en.SetupWindowTitle=Setup - [name/ver]
ja.SetupWindowTitle=セットアップ - [name/ver]
zhCN.SetupWindowTitle=安装 - [name/ver]
zhTW.SetupWindowTitle=安裝 - [name/ver]
ko.SetupWindowTitle=설치 - [name/ver]

en.WelcomeLabel2=This will install [name/ver] to your game.%n%nPlease close SiNiSistar2 before continuing.
ja.WelcomeLabel2=[name/ver] をゲームにインストールします。%n%n続行する前に SiNiSistar2 を終了してください。
zhCN.WelcomeLabel2=此向导将把 [name/ver] 安装到你的游戏中。%n%n继续前请确保已关闭 SiNiSistar2。
zhTW.WelcomeLabel2=此精靈會將 [name/ver] 安裝到你的遊戲中。%n%n繼續前請先關閉 SiNiSistar2。
ko.WelcomeLabel2=[name/ver]을(를) 게임에 설치합니다.%n%n계속하기 전에 SiNiSistar2를 종료해 주세요.

en.SelectDirLabel3=Select the game folder containing SiNiSistar2.exe.
ja.SelectDirLabel3=SiNiSistar2.exe が含まれるゲームフォルダを選択してください。
zhCN.SelectDirLabel3=请选择包含 SiNiSistar2.exe 的游戏目录。
zhTW.SelectDirLabel3=請選擇包含 SiNiSistar2.exe 的遊戲目錄。
ko.SelectDirLabel3=SiNiSistar2.exe가 있는 게임 폴더를 선택하세요.

en.ReadyLabel1=Setup is now ready to begin installing [name] to your game.
ja.ReadyLabel1=[name] をゲームにインストールする準備が整いました。
zhCN.ReadyLabel1=安装程序已准备好将 [name] 安装到你的游戏中。
zhTW.ReadyLabel1=安裝程式已準備好將 [name] 安裝到你的遊戲中。
ko.ReadyLabel1=[name]을(를) 게임에 설치할 준비가 되었습니다.

en.FinishedLabelNoIcons=Setup has finished installing [name] to your game.%n%nStart the game and configure HeatSeekar in the native Settings and Graphics pages.
ja.FinishedLabelNoIcons=[name] のゲームへのインストールが完了しました。%n%nゲームを起動し、ゲーム内の設定・グラフィックメニューから HeatSeekar を設定してください。
zhCN.FinishedLabelNoIcons=[name] 已成功安装到你的游戏中。%n%n启动游戏后，可在原生设置与图形菜单中配置 HeatSeekar。
zhTW.FinishedLabelNoIcons=[name] 已成功安裝到你的遊戲中。%n%n啟動遊戲後，可在原生設定與圖形選單中設定 HeatSeekar。
ko.FinishedLabelNoIcons=[name]이(가) 게임에 성공적으로 설치되었습니다.%n%n게임을 실행한 뒤 게임 내 설정 및 그래픽 메뉴에서 HeatSeekar를 설정할 수 있습니다.

en.FinishedLabel=Setup has finished installing [name] to your game.%n%nStart the game and configure HeatSeekar in the native Settings and Graphics pages.
ja.FinishedLabel=[name] のゲームへのインストールが完了しました。%n%nゲームを起動し、ゲーム内の設定・グラフィックメニューから HeatSeekar を設定してください。
zhCN.FinishedLabel=[name] 已成功安装到你的游戏中。%n%n启动游戏后，可在原生设置与图形菜单中配置 HeatSeekar。
zhTW.FinishedLabel=[name] 已成功安裝到你的遊戲中。%n%n啟動遊戲後，可在原生設定與圖形選單中設定 HeatSeekar。
ko.FinishedLabel=[name]이(가) 게임에 성공적으로 설치되었습니다.%n%n게임을 실행한 뒤 게임 내 설정 및 그래픽 메뉴에서 HeatSeekar를 설정할 수 있습니다.

[CustomMessages]
en.InvalidGameDir=SiNiSistar2.exe or GameAssembly.dll was not found in the selected folder. Please choose a valid SiNiSistar2 directory.
ja.InvalidGameDir=選択したフォルダに SiNiSistar2.exe または GameAssembly.dll が見つかりません。正しい SiNiSistar2 のフォルダを選択してください。
zhCN.InvalidGameDir=所选目录中未找到 SiNiSistar2.exe 或 GameAssembly.dll，请选择正确的 SiNiSistar2 客户端目录。
zhTW.InvalidGameDir=所選資料夾中找不到 SiNiSistar2.exe 或 GameAssembly.dll，請選擇正確的 SiNiSistar2 遊戲目錄。
ko.InvalidGameDir=선택한 폴더에서 SiNiSistar2.exe 또는 GameAssembly.dll을 찾을 수 없습니다. 올바른 SiNiSistar2 폴더를 선택하세요.

en.GameRunning=SiNiSistar2 is currently running from this folder. Please close the game before continuing.
ja.GameRunning=このフォルダの SiNiSistar2 が現在実行中です。続行する前にゲームを終了してください。
zhCN.GameRunning=检测到 SiNiSistar2 正在该目录运行。请先关闭游戏再继续。
zhTW.GameRunning=偵測到 SiNiSistar2 正在此目錄執行中。請先關閉遊戲再繼續。
ko.GameRunning=이 폴더의 SiNiSistar2가 현재 실행 중입니다. 계속하기 전에 게임을 종료하세요.

en.UpdateAvailable=A newer version of HeatSeekar (v%1) is available on GitHub (this installer is v%2).%n%nOpen the GitHub Releases page to download it now?
ja.UpdateAvailable=GitHub に新しいバージョンの HeatSeekar (v%1) が公開されています（このインストーラーは v%2 です）。%n%nGitHub のリリースページを開いてダウンロードしますか？
zhCN.UpdateAvailable=检测到 GitHub 上已发布 HeatSeekar 新版本 v%1（当前安装程序为 v%2）。%n%n是否前往 GitHub Releases 页面下载最新版？
zhTW.UpdateAvailable=偵測到 GitHub 上已發布 HeatSeekar 新版本 v%1（目前安裝程式為 v%2）。%n%n是否前往 GitHub Releases 頁面下載最新版本？
ko.UpdateAvailable=GitHub에 새로운 버전의 HeatSeekar (v%1)가 출시되었습니다 (현재 설치 프로그램: v%2).%n%nGitHub Releases 페이지를 열어 다운로드하시겠습니까?

en.OpenProjectPage=Open the HeatSeekar project page
ja.OpenProjectPage=HeatSeekar プロジェクトページを開く
zhCN.OpenProjectPage=打开 HeatSeekar 项目主页
zhTW.OpenProjectPage=開啟 HeatSeekar 專案首頁
ko.OpenProjectPage=HeatSeekar 프로젝트 페이지 열기

en.UninstallKeepUserFiles=Keep your personal settings, key bindings, and custom (.custom) files?
ja.UninstallKeepUserFiles=個人の設定ファイル、キー割り当て、およびカスタム (.custom) ファイルを保持しますか？
zhCN.UninstallKeepUserFiles=是否保留你的个人配置文件、按键绑定以及自定义 (.custom) 备份文件？
zhTW.UninstallKeepUserFiles=是否保留你的個人設定檔、按鍵綁定以及自訂 (.custom) 備份檔案？
ko.UninstallKeepUserFiles=개인 설정, 키 바인딩 및 사용자 지정(.custom) 파일을 유지하시겠습니까?

en.UninstallRemoveBepInEx=BepInEx was installed by this setup and no other plugins were found. Remove BepInEx as well?
ja.UninstallRemoveBepInEx=BepInEx は本インストーラーによって導入され、他のプラグインは検出されませんでした。BepInEx もあわせて削除しますか？
zhCN.UninstallRemoveBepInEx=检测到本安装器曾在此目录部署 BepInEx，且当前没有其他插件。是否一并卸载并清理 BepInEx？
zhTW.UninstallRemoveBepInEx=偵測到本安裝程式曾在此目錄部署 BepInEx，且目前沒有其他外掛。是否一併解除安裝並清理 BepInEx？
ko.UninstallRemoveBepInEx=BepInEx가 이 설치 프로그램에 의해 설치되었으며 다른 플러그인이 없습니다. BepInEx도 함께 제거하시겠습니까?

[Files]
Source: "{#ModStageDir}\official-hashes.ini"; Flags: dontcopy

; BepInEx 6 IL2CPP x64 runtime (only installed when missing or incomplete)
Source: "{#BepInExStageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs uninsneveruninstall; Check: ShouldInstallBepInEx; AfterInstall: MarkBepInExInstalled
Source: "licenses\BepInEx-LICENSE.txt"; DestDir: "{app}\BepInEx"; DestName: "LICENSE-BepInEx.txt"; Flags: ignoreversion uninsneveruninstall; Check: ShouldInstallBepInEx

; HeatSeekar core DLL
Source: "{#ModStageDir}\BepInEx\plugins\HeatSeekar\SiNiSistar2.HeatSeekar.dll"; DestDir: "{app}\BepInEx\plugins\HeatSeekar"; Flags: ignoreversion

; HeatSeekar assets & localization (auto-diffs and renames user-modified files to .custom before writing new version)
Source: "{#ModStageDir}\BepInEx\plugins\HeatSeekar\assets\*"; DestDir: "{app}\BepInEx\plugins\HeatSeekar\assets"; Flags: ignoreversion recursesubdirs createallsubdirs; BeforeInstall: BackupIfCustomized; AfterInstall: RecordOfficialHash
Source: "{#ModStageDir}\BepInEx\plugins\HeatSeekar\localization\*"; DestDir: "{app}\BepInEx\plugins\HeatSeekar\localization"; Flags: ignoreversion recursesubdirs createallsubdirs; BeforeInstall: BackupIfCustomized; AfterInstall: RecordOfficialHash

[Run]
Filename: "https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/"; Description: "{cm:OpenProjectPage}"; Flags: postinstall shellexec runasoriginaluser unchecked

[Code]
const
  LatestReleaseApiUrl = 'https://api.github.com/repos/akiloneus/SiNiSistar2-HeatSeekar/releases/latest';
  ReleasesPageUrl = 'https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest';
  StateIniRelPath = 'BepInEx\plugins\HeatSeekar\install-state.ini';

var
  BepInExDecisionCached: Boolean;
  BepInExDecisionValue: Boolean;

function IsValidGameDir(const Dir: string): Boolean;
begin
  Result := (Dir <> '') and FileExists(AddBackslash(Dir) + 'SiNiSistar2.exe') and FileExists(AddBackslash(Dir) + 'GameAssembly.dll');
end;

function IsGameRunningInDir(const Dir: string): Boolean;
var
  WbemLocator, WbemServices, Processes, ProcessItem: Variant;
  TargetExe: string;
  Count, I: Integer;
begin
  Result := False;
  TargetExe := ExpandFileName(AddBackslash(Dir) + 'SiNiSistar2.exe');
  try
    WbemLocator := CreateOleObject('WbemScripting.SWbemLocator');
    WbemServices := WbemLocator.ConnectServer('.', 'root\cimv2');
    Processes := WbemServices.ExecQuery('SELECT ExecutablePath FROM Win32_Process WHERE Name = "SiNiSistar2.exe"');
    Count := Processes.Count;
    for I := 0 to Count - 1 do
    begin
      ProcessItem := Processes.ItemIndex(I);
      if not VarIsNull(ProcessItem.ExecutablePath) then
      begin
        if CompareText(ExpandFileName(String(ProcessItem.ExecutablePath)), TargetExe) = 0 then
        begin
          Result := True;
          Exit;
        end;
      end;
    end;
  except
    Result := False;
  end;
end;

procedure ConsiderCandidate(const Dir: string; var BestDir: string; var BestTime: TFileTime; var HasBest: Boolean);
var
  FindRec: TFindRec;
  Cmp: Integer;
begin
  if not IsValidGameDir(Dir) then Exit;
  if not FindFirst(AddBackslash(Dir) + 'GameAssembly.dll', FindRec) then Exit;
  try
    if not HasBest then
    begin
      BestDir := Dir;
      BestTime := FindRec.LastWriteTime;
      HasBest := True;
    end
    else
    begin
      if FindRec.LastWriteTime.dwHighDateTime > BestTime.dwHighDateTime then Cmp := 1
      else if FindRec.LastWriteTime.dwHighDateTime < BestTime.dwHighDateTime then Cmp := -1
      else if FindRec.LastWriteTime.dwLowDateTime > BestTime.dwLowDateTime then Cmp := 1
      else if FindRec.LastWriteTime.dwLowDateTime < BestTime.dwLowDateTime then Cmp := -1
      else Cmp := 0;
      if Cmp > 0 then
      begin
        BestDir := Dir;
        BestTime := FindRec.LastWriteTime;
      end;
    end;
  finally
    FindClose(FindRec);
  end;
end;

function ProbeCandidateParent(const ParentDir: string): string;
var
  FindRec, SubRec, SubRec2: TFindRec;
  Dir1, Dir2, Dir3, BestDir: string;
  BestTime: TFileTime;
  HasBest: Boolean;
begin
  Result := '';
  BestDir := '';
  HasBest := False;
  if not DirExists(ParentDir) then Exit;
  if FindFirst(AddBackslash(ParentDir) + '*SiNiSistar*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
          begin
            Dir1 := AddBackslash(ParentDir) + FindRec.Name;
            if IsValidGameDir(Dir1) then
            begin
              ConsiderCandidate(Dir1, BestDir, BestTime, HasBest);
            end
            else if FindFirst(AddBackslash(Dir1) + '*', SubRec) then
            begin
              try
                repeat
                  if ((SubRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0) and (SubRec.Name <> '.') and (SubRec.Name <> '..') then
                  begin
                    Dir2 := AddBackslash(Dir1) + SubRec.Name;
                    if IsValidGameDir(Dir2) then
                    begin
                      ConsiderCandidate(Dir2, BestDir, BestTime, HasBest);
                    end
                    else if FindFirst(AddBackslash(Dir2) + '*', SubRec2) then
                    begin
                      try
                        repeat
                          if ((SubRec2.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0) and (SubRec2.Name <> '.') and (SubRec2.Name <> '..') then
                          begin
                            Dir3 := AddBackslash(Dir2) + SubRec2.Name;
                            ConsiderCandidate(Dir3, BestDir, BestTime, HasBest);
                          end;
                        until not FindNext(SubRec2);
                      finally
                        FindClose(SubRec2);
                      end;
                    end;
                  end;
                until not FindNext(SubRec);
              finally
                FindClose(SubRec);
              end;
            end;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
  Result := BestDir;
end;

function FindInSteamLibraries(const SteamRoot: string): string;
var
  VdfPath, Line, LibPath, Candidate: string;
  Lines: TArrayOfString;
  I, P1, P2: Integer;
begin
  Result := '';
  if (SteamRoot = '') or (not DirExists(SteamRoot)) then Exit;
  Candidate := ProbeCandidateParent(AddBackslash(SteamRoot) + 'steamapps\common');
  if Candidate <> '' then begin Result := Candidate; Exit; end;

  VdfPath := AddBackslash(SteamRoot) + 'steamapps\libraryfolders.vdf';
  if not FileExists(VdfPath) then Exit;
  if LoadStringsFromFile(VdfPath, Lines) then
  begin
    for I := 0 to GetArrayLength(Lines) - 1 do
    begin
      Line := Trim(Lines[I]);
      if Pos('"path"', Lowercase(Line)) = 1 then
      begin
        Delete(Line, 1, 6);
        P1 := Pos('"', Line);
        if P1 > 0 then
        begin
          Delete(Line, 1, P1);
          P2 := Pos('"', Line);
          if P2 > 0 then
          begin
            LibPath := Copy(Line, 1, P2 - 1);
            StringChangeEx(LibPath, '\\', '\', True);
            Candidate := ProbeCandidateParent(AddBackslash(LibPath) + 'steamapps\common');
            if Candidate <> '' then begin Result := Candidate; Exit; end;
          end;
        end;
      end;
    end;
  end;
end;

function GetDefaultGameDir(Param: string): string;
var
  SteamPath, Candidate, Root: string;
  DriveChar: Integer;
begin
  if RegQueryStringValue(HKCU, 'Software\Valve\Steam', 'SteamPath', SteamPath) then
  begin
    StringChangeEx(SteamPath, '/', '\', True);
    Candidate := FindInSteamLibraries(SteamPath);
    if Candidate <> '' then begin Result := Candidate; Exit; end;
  end;

  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Valve\Steam', 'InstallPath', SteamPath) then
  begin
    Candidate := FindInSteamLibraries(SteamPath);
    if Candidate <> '' then begin Result := Candidate; Exit; end;
  end;

  for DriveChar := Ord('C') to Ord('Z') do
  begin
    Root := Chr(DriveChar) + ':\';
    if DirExists(Root) then
    begin
      Candidate := ProbeCandidateParent(Root + 'DLsite\Games');
      if Candidate <> '' then begin Result := Candidate; Exit; end;
      Candidate := ProbeCandidateParent(Root + 'DLsite');
      if Candidate <> '' then begin Result := Candidate; Exit; end;
      Candidate := ProbeCandidateParent(Root + 'Games');
      if Candidate <> '' then begin Result := Candidate; Exit; end;
      Candidate := ProbeCandidateParent(Root + 'SteamLibrary\steamapps\common');
      if Candidate <> '' then begin Result := Candidate; Exit; end;
    end;
  end;

  Result := 'C:\Games\SiNiSistar2';
end;

function HasCompatibleBepInEx(const GameDir: string): Boolean;
var
  Base: string;
begin
  Base := AddBackslash(GameDir);
  Result :=
    FileExists(Base + 'winhttp.dll') and
    FileExists(Base + 'doorstop_config.ini') and
    FileExists(Base + '.doorstop_version') and
    FileExists(Base + 'BepInEx\core\BepInEx.Core.dll') and
    FileExists(Base + 'BepInEx\core\BepInEx.Unity.IL2CPP.dll') and
    FileExists(Base + 'BepInEx\core\Il2CppInterop.Runtime.dll') and
    FileExists(Base + 'BepInEx\core\0Harmony.dll') and
    FileExists(Base + 'dotnet\coreclr.dll') and
    FileExists(Base + 'dotnet\System.Private.CoreLib.dll');
end;

function ShouldInstallBepInEx: Boolean;
begin
  if not BepInExDecisionCached then
  begin
    BepInExDecisionValue := not HasCompatibleBepInEx(ExpandConstant('{app}'));
    BepInExDecisionCached := True;
  end;
  Result := BepInExDecisionValue;
end;

procedure MarkBepInExInstalled;
begin
  SetIniString('State', 'InstalledBepInEx', '1', ExpandConstant('{app}\') + StateIniRelPath);
end;

function GetRelativeToHeatSeekar(const FullTargetPath: string): string;
var
  Prefix: string;
begin
  Prefix := ExpandConstant('{app}\BepInEx\plugins\HeatSeekar\');
  if Pos(Lowercase(Prefix), Lowercase(FullTargetPath)) = 1 then
    Result := Copy(FullTargetPath, Length(Prefix) + 1, MaxInt)
  else
    Result := ExtractFileName(FullTargetPath);
end;

procedure BackupIfCustomized;
var
  TargetFile, RelKey, ExistingHash, NewHash, PrevHash: string;
begin
  TargetFile := ExpandConstant(CurrentFileName);
  if not FileExists(TargetFile) then Exit;

  RelKey := GetRelativeToHeatSeekar(TargetFile);
  try
    ExistingHash := Lowercase(GetSHA256OfFile(TargetFile));
  except
    Exit;
  end;

  NewHash := Lowercase(GetIniString('Hashes', RelKey, '', ExpandConstant('{tmp}\official-hashes.ini')));
  PrevHash := Lowercase(GetIniString('Hashes', RelKey, '', ExpandConstant('{app}\') + StateIniRelPath));

  if (NewHash <> '') and (ExistingHash <> NewHash) and ((PrevHash = '') or (ExistingHash <> PrevHash)) then
  begin
    CopyFile(TargetFile, TargetFile + '.custom', False);
  end;
end;

procedure RecordOfficialHash;
var
  TargetFile, RelKey, NewHash: string;
begin
  TargetFile := ExpandConstant(CurrentFileName);
  if not FileExists(TargetFile) then Exit;

  RelKey := GetRelativeToHeatSeekar(TargetFile);
  try
    NewHash := Lowercase(GetSHA256OfFile(TargetFile));
    SetIniString('Hashes', RelKey, NewHash, ExpandConstant('{app}\') + StateIniRelPath);
  except
  end;
end;

function ExtractJsonString(const Json, Key: string; StartPos: Integer; out NextPos: Integer): string;
var
  Token: string;
  P, Q: Integer;
begin
  Result := '';
  NextPos := 0;
  Token := '"' + Key + '"';
  P := Pos(Token, Copy(Json, StartPos, MaxInt));
  if P = 0 then Exit;
  P := StartPos + P + Length(Token) - 1;
  P := P + Pos(':', Copy(Json, P, MaxInt));
  while (P <= Length(Json)) and ((Json[P] = ' ') or (Json[P] = #9) or (Json[P] = #13) or (Json[P] = #10)) do
    P := P + 1;
  if (P > Length(Json)) or (Json[P] <> '"') then Exit;
  P := P + 1;
  Q := P;
  while (Q <= Length(Json)) and (Json[Q] <> '"') do
    Q := Q + 1;
  Result := Copy(Json, P, Q - P);
  NextPos := Q + 1;
end;

function CompareSemVerSimple(const VerA, VerB: string): Integer;
var
  A, B: Int64;
begin
  Result := 0;
  if StrToVersion(VerA, A) and StrToVersion(VerB, B) then
    Result := ComparePackedVersion(A, B);
end;

function StripTagPrefix(const RawTag: string): string;
var
  PlusPos, DashPos: Integer;
begin
  Result := Trim(RawTag);
  if (Length(Result) > 1) and ((Result[1] = 'v') or (Result[1] = 'V')) then
    Delete(Result, 1, 1);
  PlusPos := Pos('+', Result);
  if PlusPos > 0 then Result := Copy(Result, 1, PlusPos - 1);
  DashPos := Pos('-', Result);
  if DashPos > 0 then Result := Copy(Result, 1, DashPos - 1);
end;

function QueryLatestGitHubVersion(out LatestVer, ReleaseUrl: string): Boolean;
var
  Http: Variant;
  Resp, Tag, HtmlUrl: string;
  NextPos: Integer;
begin
  Result := False;
  LatestVer := '';
  ReleaseUrl := ReleasesPageUrl;
  try
    Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    Http.SetTimeouts(3000, 3000, 4000, 4000);
    Http.Open('GET', LatestReleaseApiUrl, False);
    Http.SetRequestHeader('User-Agent', 'HeatSeekar-Installer/{#MyAppVersion}');
    Http.SetRequestHeader('Accept', 'application/vnd.github+json');
    Http.Send('');
    if Http.Status <> 200 then Exit;
    Resp := String(Http.ResponseText);
    Tag := ExtractJsonString(Resp, 'tag_name', 1, NextPos);
    LatestVer := StripTagPrefix(Tag);
    if LatestVer = '' then Exit;
    HtmlUrl := ExtractJsonString(Resp, 'html_url', 1, NextPos);
    if HtmlUrl <> '' then ReleaseUrl := HtmlUrl;
    Result := True;
  except
    Result := False;
  end;
end;

function InitializeSetup: Boolean;
var
  LatestVer, ReleaseUrl: string;
  ErrorCode: Integer;
begin
  Result := True;
  if not WizardSilent then
  begin
    if QueryLatestGitHubVersion(LatestVer, ReleaseUrl) then
    begin
      if CompareSemVerSimple(LatestVer, '{#MyAppVersion}') > 0 then
      begin
        if SuppressibleMsgBox(FmtMessage(CustomMessage('UpdateAvailable'), [LatestVer, '{#MyAppVersion}']), mbConfirmation, MB_YESNO, IDNO) = IDYES then
        begin
          ShellExec('open', ReleaseUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
          Result := False;
          Exit;
        end;
      end;
    end;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Dir: string;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    Dir := WizardDirValue;
    if not IsValidGameDir(Dir) then
    begin
      SuppressibleMsgBox(CustomMessage('InvalidGameDir'), mbError, MB_OK, IDOK);
      Result := False;
      Exit;
    end;
    if IsGameRunningInDir(Dir) then
    begin
      SuppressibleMsgBox(CustomMessage('GameRunning'), mbError, MB_OK, IDOK);
      Result := False;
      Exit;
    end;
    BepInExDecisionCached := False;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  ExtractTemporaryFile('official-hashes.ini');
end;

function HasOtherBepInExPlugins(const GameDir: string): Boolean;
var
  PluginsDir: string;
  FindRec, SubRec: TFindRec;
begin
  Result := False;
  PluginsDir := AddBackslash(GameDir) + 'BepInEx\plugins';
  if not DirExists(PluginsDir) then Exit;

  if FindFirst(AddBackslash(PluginsDir) + '*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
        begin
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
          begin
            if CompareText(FindRec.Name, 'HeatSeekar') <> 0 then
            begin
              if FindFirst(AddBackslash(PluginsDir) + FindRec.Name + '\*.dll', SubRec) then
              begin
                FindClose(SubRec);
                Result := True;
                Exit;
              end;
            end;
          end
          else if CompareText(ExtractFileExt(FindRec.Name), '.dll') = 0 then
          begin
            Result := True;
            Exit;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDir, ModDir, StateIni: string;
  InstalledBep: Boolean;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDir := ExpandConstant('{app}');
    ModDir := AddBackslash(AppDir) + 'BepInEx\plugins\HeatSeekar';
    StateIni := AddBackslash(AppDir) + StateIniRelPath;
    InstalledBep := GetIniString('State', 'InstalledBepInEx', '0', StateIni) = '1';

    DeleteFile(StateIni);

    if SuppressibleMsgBox(CustomMessage('UninstallKeepUserFiles'), mbConfirmation, MB_YESNO, IDYES) = IDNO then
    begin
      DelTree(ModDir, True, True, True);
    end
    else
    begin
      RemoveDir(ModDir + '\assets');
      RemoveDir(ModDir + '\localization');
      RemoveDir(ModDir);
    end;

    if InstalledBep and (not HasOtherBepInExPlugins(AppDir)) then
    begin
      if SuppressibleMsgBox(CustomMessage('UninstallRemoveBepInEx'), mbConfirmation, MB_YESNO, IDYES) = IDYES then
      begin
        DeleteFile(AddBackslash(AppDir) + 'winhttp.dll');
        DeleteFile(AddBackslash(AppDir) + 'doorstop_config.ini');
        DeleteFile(AddBackslash(AppDir) + '.doorstop_version');
        DeleteFile(AddBackslash(AppDir) + 'changelog.txt');
        DeleteFile(AddBackslash(AppDir) + 'BepInEx\LICENSE-BepInEx.txt');
        DeleteFile(AddBackslash(AppDir) + 'BepInEx\LogOutput.log');
        DeleteFile(AddBackslash(AppDir) + 'BepInEx\config\BepInEx.cfg');
        DelTree(AddBackslash(AppDir) + 'dotnet', True, True, True);
        DelTree(AddBackslash(AppDir) + 'BepInEx\core', True, True, True);
        DelTree(AddBackslash(AppDir) + 'BepInEx\interop', True, True, True);
        DelTree(AddBackslash(AppDir) + 'BepInEx\cache', True, True, True);
        DelTree(AddBackslash(AppDir) + 'BepInEx\patchers', True, True, True);
        RemoveDir(AddBackslash(AppDir) + 'BepInEx\config');
        RemoveDir(AddBackslash(AppDir) + 'BepInEx\plugins');
        RemoveDir(AddBackslash(AppDir) + 'BepInEx');
      end;
    end;
  end;
end;
