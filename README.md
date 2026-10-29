# SeleniumBasic KTOOL

[florentbr/SeleniumBasic 2.0.9.0](https://github.com/florentbr/SeleniumBasic)에서 파생한 Windows 10/11용 VBA·COM 라이브러리입니다. 원본 BSD 라이선스와 저작권을 유지합니다.

## 설치

Releases의 `SeleniumBasic-KTOOL-2.0.9.1.exe`를 실행하면 선택창 없이 설치합니다. .NET Framework 4.8이 없으면 Microsoft 배포본으로 설치하고, 설치된 Chrome·Edge 버전에 맞는 드라이버를 자동으로 준비합니다. 설치·드라이버 갱신은 백그라운드로 실행합니다.

사용자별 `%LOCALAPPDATA%\SeleniumBasic`에 설치하며 기존 사용자 프로필·업무 파일을 일괄 삭제하지 않습니다. 기존 COM GUID, ProgID, 타입 라이브러리 버전 2.0, 어셈블리 이름·버전·서명은 유지합니다.

## 수정 사항

- 팝업이 스스로 닫힌 뒤 살아 있는 기존 창으로 복구하고 정보를 다시 읽습니다.
- 저장해 둔 창 객체를 활성화할 때 실제 브라우저도 전환합니다.
- 창 두 개일 때 이전 창을 제외해서 전환이 실패하던 문제를 수정했습니다.
- 제목 검색 실패 뒤 원격 창과 내부 캐시가 달라지던 문제를 수정했습니다.
- 통신 시간 제한을 실행 중인 세션과 응답 본문 읽기에도 적용합니다.
- Edge의 인수·프로필·환경 설정을 Chromium Edge에 전달합니다.
- Chrome·Edge 시작 때 버전을 확인하고 맞는 드라이버가 없으면 공식 배포본으로 갱신합니다. 검증한 파일로 교체하며 실패 원인은 로그와 VBA 오류 정보로 제공합니다.
- Windows 이벤트 로그 쓰기 권한 때문에 원래 오류가 가려지지 않게 했습니다.

## VBA 창 핸들과 오류 조회

```vb
Dim home As Selenium.Window
Set home = driver.Window
Debug.Print home.Handle          'WebDriver 창/탭 식별자
Debug.Print home.NativeHandle    '실제 Windows HWND(10진수 문자열)
Debug.Print driver.WindowHandles.Count

'팝업 종료 후 원본 창으로 정확하게 전환
Call driver.SwitchToWindowByHandle(home.Handle, 5000)
Set item = driver.FindElementById("your-element-id")
```

같은 OS 창의 탭은 HWND를 공유할 수 있습니다. `NativeHandle`은 제어 중인 로컬 브라우저 프로세스에서 창을 유일하게 식별한 경우에만 반환하며, 닫힘·원격·모호한 경우 `"0"`입니다. 페이지가 다시 로드되면 기존 WebElement를 재사용하지 말고 다시 찾습니다.

```vb
On Error GoTo Failed
Call driver.Start("chrome")
Exit Sub
Failed:
    Debug.Print driver.LastErrorCode
    Debug.Print driver.LastErrorMessage
    Debug.Print driver.LastErrorStage
    Debug.Print driver.LastErrorTime
    Debug.Print driver.LastErrorJson
    driver.ClearLastError
```

자동 드라이버 준비 실패 코드는 `1001`입니다. 기존 WebDriver 코드(예: 창 없음 23, 대화상자 열림 26)와 `Err.Number`·`Err.Description`도 제공합니다. 성공적으로 복구한 작업은 오류로 남기지 않습니다. 클릭·스크립트·전송 작업은 자동으로 중복 실행하지 않습니다.

## 로그

- `%LOCALAPPDATA%\KTOOL\SetupLogs\SeleniumBasic_KTOOL_setup.log`
- 설치 폴더의 `KTOOL_DriverSetup.log`
- 설치 폴더의 `KTOOL_chrome_driver.json`, `KTOOL_edge_driver.json`

## 빌드와 검사

```powershell
.\KTOOL\Build.ps1 -Test
```

컴파일러·참조 어셈블리·원본 배포 자료는 확인값을 검사해 작업 폴더로 내려받습니다. 빌드 도구를 시스템에 설치하지 않습니다. 결과는 `KTOOL\dist`에 생성됩니다.

창 종료·복구·두 창 전환·시간 제한·오류 조회를 로컬 가짜 서버로 재현해 32·64비트에서 검사합니다. `KTOOL/Tests/RealPopupTest.cs`는 별도 시험 프로필의 실제 Chrome·Edge로 팝업 종료, 본창 정보 조회, 핸들·HWND를 검증합니다. 원본은 최초 13개 재현에서 실패했고 수정본은 통과했습니다.

## 범위

KTOOL 프로그램 업데이트 시 기존 Selenium을 삭제·교체하는 연동은 별도 작업입니다. 이 저장소는 Selenium 라이브러리·무인 설치·브라우저 드라이버 준비를 제공합니다.