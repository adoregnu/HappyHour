# ProgressWindow 사용 가이드

파일 및 폴더 **복사/이동** 진행 상황을 표시하는 `ProgressWindow`와 `FileCopyUtility`의 사용 방법을 설명합니다.

## 주요 기능

### ProgressWindow
- 전체 복사/이동 진행률 표시 (프로그래스 바)
- 현재 파일별 진행률 표시
- 복사/이동 속도 및 남은 시간 계산
- 처리된 파일 수 및 크기 통계
- 취소 기능 지원
- 사용자 친화적인 한국어 인터페이스

### FileCopyUtility
- **파일 복사/이동**: 여러 파일 동시 처리
- **폴더 복사/이동**: 폴더 구조를 포함한 재귀적 처리
- **혼합 처리**: 파일과 폴더를 함께 복사/이동
- **최적화된 이동**: 같은 드라이브에서 빠른 이동 연산 사용
- 진행 상황 실시간 업데이트
- 파일 덮어쓰기 옵션
- 취소 및 오류 처리
- 비동기 처리로 UI 블록 방지

## 기본 사용법

### 1. 파일 복사/이동

```csharp
using HappyHour.Utilities;

// 복사할 파일 목록
var sourceFiles = new List<string>
{
    @"C:\source\file1.txt",
    @"C:\source\file2.jpg", 
    @"C:\source\file3.mp4"
};

var destinationFolder = @"C:\destination";

// 파일 복사
var copySuccess = await FileCopyUtility.CopyFilesWithProgressAsync(
    sourceFiles, 
    destinationFolder, 
    overwrite: true,
    owner: this
);

// 파일 이동
var moveSuccess = await FileCopyUtility.MoveFilesWithProgressAsync(
    sourceFiles, 
    destinationFolder, 
    overwrite: true,
    owner: this
);
```

### 2. 폴더 복사/이동

```csharp
// 폴더 복사 (하위 폴더 포함)
var copySuccess = await FileCopyUtility.CopyFolderWithProgressAsync(
    sourceFolder: @"C:\source\MyFolder",
    destinationFolder: @"C:\destination\MyFolder",
    overwrite: true,
    includeSubFolders: true,
    owner: this
);

// 폴더 이동 (빠른 이동 최적화 자동 적용)
var moveSuccess = await FileCopyUtility.MoveFolderWithProgressAsync(
    sourceFolder: @"C:\source\MyFolder",
    destinationFolder: @"D:\destination\MyFolder",  // 다른 드라이브로 이동
    overwrite: true,
    includeSubFolders: true,
    owner: this
);

// 같은 드라이브에서 폴더 이동 (즉시 완료)
var fastMoveSuccess = await FileCopyUtility.MoveFolderWithProgressAsync(
    sourceFolder: @"C:\source\MyFolder",
    destinationFolder: @"C:\destination\MyFolder",  // 같은 드라이브
    overwrite: false,
    includeSubFolders: true,
    owner: this
);
```

### 3. 혼합 항목 복사/이동

```csharp
// 파일과 폴더를 함께 처리
var sourceItems = new List<string>
{
    @"C:\source\file1.txt",        // 개별 파일
    @"C:\source\MyFolder",         // 전체 폴더
    @"C:\source\document.pdf",     // 개별 파일
    @"C:\source\AnotherFolder"     // 전체 폴더
};

// 혼합 복사
var copySuccess = await FileCopyUtility.CopyItemsWithProgressAsync(
    sourcePaths: sourceItems,
    destinationFolder: @"C:\destination",
    overwrite: true,
    includeSubFolders: true,
    owner: this
);

// 혼합 이동
var moveSuccess = await FileCopyUtility.MoveItemsWithProgressAsync(
    sourcePaths: sourceItems,
    destinationFolder: @"D:\destination",
    overwrite: true,
    includeSubFolders: true,
    owner: this
);
```

## 이동 최적화 기능

### 빠른 이동 (같은 드라이브)
```csharp
// C: 드라이브 내에서 이동 - 즉시 완료
await FileCopyUtility.MoveFolderWithProgressAsync(
    @"C:\temp\large_folder",
    @"C:\backup\large_folder",
    overwrite: true,
    includeSubFolders: true,
    owner: this
);
// ? 파일 시스템 레벨에서 즉시 이동 (Directory.Move 사용)
```

### 크로스 드라이브 이동
```csharp
// C: 드라이브에서 D: 드라이브로 이동 - 복사 후 삭제
await FileCopyUtility.MoveFolderWithProgressAsync(
    @"C:\temp\large_folder",
    @"D:\backup\large_folder",
    overwrite: true,
    includeSubFolders: true,
    owner: this
);
// ? 복사 진행률 표시 → 완료 후 원본 자동 삭제
```

## 고급 사용법

### 개별 ProgressWindow 제어

```csharp
// 수동으로 ProgressWindow 생성 및 제어
var progressWindow = new ProgressWindow();
progressWindow.Owner = this;
progressWindow.Title = "커스텀 이동 작업";

// 진행 상황 초기화
progressWindow.InitializeProgress(100, 1024 * 1024 * 1024);
progressWindow.Show();

// 현재 처리 중인 항목 업데이트
progressWindow.UpdateCurrentFile("폴더 이동: Documents/Images");

// 처리 완료 알림
progressWindow.CompleteFile();

// 취소 여부 확인
if (progressWindow.IsCancelled)
{
    // 작업 중단 및 정리
}
```

### 조건부 처리 로직

```csharp
public async Task<bool> SmartMoveFiles(List<string> files, string destination)
{
    // 같은 드라이브 파일과 다른 드라이브 파일 분리
    var sameDriveFiles = new List<string>();
    var crossDriveFiles = new List<string>();
    
    var destDrive = Path.GetPathRoot(destination);
    
    foreach (var file in files)
    {
        var sourceDrive = Path.GetPathRoot(file);
        if (string.Equals(sourceDrive, destDrive, StringComparison.OrdinalIgnoreCase))
        {
            sameDriveFiles.Add(file);
        }
        else
        {
            crossDriveFiles.Add(file);
        }
    }
    
    // 같은 드라이브 파일들은 빠르게 이동
    if (sameDriveFiles.Any())
    {
        var result1 = await FileCopyUtility.MoveFilesWithProgressAsync(
            sameDriveFiles, destination, overwrite: true, owner: this);
        if (!result1) return false;
    }
    
    // 다른 드라이브 파일들은 복사 후 삭제로 이동
    if (crossDriveFiles.Any())
    {
        var result2 = await FileCopyUtility.MoveFilesWithProgressAsync(
            crossDriveFiles, destination, overwrite: true, owner: this);
        if (!result2) return false;
    }
    
    return true;
}
```

## 실용적인 사용 시나리오

### 1. 파일 정리 및 정렬

```csharp
// 다운로드 폴더 정리
var downloadFiles = Directory.GetFiles(@"C:\Users\User\Downloads");
var documentFiles = downloadFiles.Where(f => 
    Path.GetExtension(f).ToLower() is ".pdf" or ".docx" or ".txt").ToList();

// 문서 파일들을 문서 폴더로 이동
await FileCopyUtility.MoveFilesWithProgressAsync(
    documentFiles,
    @"C:\Users\User\Documents\FromDownloads",
    overwrite: false,
    owner: this
);
```

### 2. 프로젝트 백업 및 이동

```csharp
// 개발 프로젝트를 다른 드라이브로 이동
var projectFolders = new[]
{
    @"C:\Projects\WebApp",
    @"C:\Projects\MobileApp",
    @"C:\Projects\Desktop"
};

await FileCopyUtility.MoveItemsWithProgressAsync(
    projectFolders,
    @"D:\Development\Projects",
    overwrite: false,
    includeSubFolders: true,
    owner: this
);
```

### 3. 미디어 파일 정리

```csharp
// 미디어 파일을 외부 드라이브로 이동
var mediaExtensions = new[] { ".mp4", ".avi", ".mkv", ".mp3", ".flac" };
var mediaFiles = Directory.GetFiles(@"C:\Users\User\Videos", "*", SearchOption.AllDirectories)
    .Where(f => mediaExtensions.Contains(Path.GetExtension(f).ToLower()))
    .ToList();

await FileCopyUtility.MoveFilesWithProgressAsync(
    mediaFiles,
    @"E:\MediaLibrary\Videos",
    overwrite: false,
    owner: this
);
```

### 4. 임시 파일 정리

```csharp
// 임시 폴더를 휴지통으로 이동 (실제로는 다른 위치로)
var tempFolders = new[]
{
    @"C:\Temp\OldProjects",
    @"C:\Temp\Downloads",
    @"C:\Temp\Extracted"
};

await FileCopyUtility.MoveItemsWithProgressAsync(
    tempFolders,
    @"C:\Recycle\TempCleanup_" + DateTime.Now.ToString("yyyyMMdd"),
    overwrite: false,
    includeSubFolders: true,
    owner: this
);
```

## 성능 및 최적화

### 이동 작업 최적화
- **같은 드라이브**: `Directory.Move()` / `File.Move()` 사용으로 즉시 완료
- **다른 드라이브**: 복사 → 검증 → 원본 삭제 순서로 안전한 이동
- **빈 폴더 정리**: 이동 완료 후 원본 빈 폴더 자동 삭제
- **원자적 연산**: 실패 시 부분 완료 상태 자동 정리

### 메모리 최적화
- **8KB 스트리밍 버퍼**: 대용량 파일도 메모리 효율적 처리
- **비동기 I/O**: UI 블록 없이 백그라운드 처리
- **점진적 업데이트**: 실시간 진행률 및 속도 계산

### 오류 복구
- **부분 실패 처리**: 일부 파일 실패 시 계속 진행
- **롤백 지원**: 이동 실패 시 부분 복사된 파일 자동 삭제
- **권한 오류 처리**: 접근 불가 파일 건너뛰기
- **경로 길이 제한**: 긴 경로 자동 감지 및 처리

## 오류 상황 및 처리

### 이동 관련 특별 오류
- **디스크 공간 부족**: 크로스 드라이브 이동 시 공간 확인
- **원본 삭제 실패**: 복사 성공 후 원본 삭제 실패 시 경고
- **폴더 병합 충돌**: 기존 폴더와 병합 시 사용자 확인
- **파일 잠금**: 사용 중인 파일 이동 실패 처리
- **순환 참조**: 폴더를 자신의 하위로 이동 시도 방지

### 자동 복구 기능
- **부분 이동 정리**: 실패 시 불완전한 상태 자동 정리
- **원본 보존**: 이동 실패 시 원본 파일 안전하게 보존
- **재시도 메커니즘**: 일시적 오류에 대한 자동 재시도
- **사용자 선택**: 충돌 상황에서 사용자 의사결정 지원

모든 기능은 사용자 친화적인 한국어 메시지와 함께 제공됩니다.