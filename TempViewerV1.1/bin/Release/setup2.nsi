# Имя выходного файла установщика
Outfile "TempViewerRPSetup.exe"

# Путь установки (по умолчанию Program Files)
InstallDir "$PROGRAMFILES\TempViewer"

# Уровень прав администратора
RequestExecutionLevel admin

# Имя приложения, издателя и версия
!define APPNAME "TempViewer"
!define COMPANYNAME "LIKSHADOW" ; замени на свою компанию, если нужно
!define APPVERSION "3.0.0.1"

VIProductVersion "${APPVERSION}"
VIAddVersionKey "ProductName" "${APPNAME}"
VIAddVersionKey "CompanyName" "${COMPANYNAME}"
VIAddVersionKey "FileVersion" "${APPVERSION}"

# Основная секция установки
Section "Install"

  # Создание папки назначения
  SetOutPath "$INSTDIR"

  # Копирование файлов
  File "TempViewer.exe"
  File "TempViewer.exe.config"
  File "TempViewer.pdb"
  
  File "System.Text.Encodings.Web.dll"
  File "System.Text.Encodings.Web.xml"
  File "System.IO.Pipelines.dll"
  File "System.IO.Pipelines.xml"
  File "Microsoft.Bcl.AsyncInterfaces.dll"
  File "Microsoft.Bcl.AsyncInterfaces.xml"
  File "System.Text.Json.dll"
  File "System.Text.Json.xml" 
  File "System.Memory.dll" 
  File "System.Memory.xml"
  File "System.Runtime.CompilerServices.Unsafe.dll"
  File "System.Runtime.CompilerServices.Unsafe.xml"
  File "System.Buffers.dll" 
  File "System.Buffers.xml" 
  File "System.Threading.Tasks.Extensions.dll"
  File "System.Threading.Tasks.Extensions.xml"
  File "System.ValueTuple.dll"
  File "System.ValueTuple.xml"
  File "System.Numerics.Vectors.dll"
  File "System.Numerics.Vectors.xml"

  # Создание ярлыка на рабочем столе
  CreateShortcut "$DESKTOP\TempViewer.lnk" "$INSTDIR\TempViewer.exe"

  # Создание ярлыка в меню Пуск
  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortcut "$SMPROGRAMS\${APPNAME}\TempViewer.lnk" "$INSTDIR\TempViewer.exe"
  CreateShortcut "$SMPROGRAMS\${APPNAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"

  # Запись информации для деинсталляции в реестр
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "${APPVERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "Publisher" "${COMPANYNAME}"

  # Создание uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

# Секция удаления
Section "Uninstall"

  # Удаление файлов
  Delete "$INSTDIR\TempViewer.exe"
  Delete "$INSTDIR\TempViewer.exe.config"
  Delete "$INSTDIR\TempViewer.pdb"
  
  Delete "$INSTDIR\System.Text.Encodings.Web.dll"
  Delete "$INSTDIR\System.Text.Encodings.Web.xml"
  Delete "$INSTDIR\System.IO.Pipelines.dll"
  Delete "$INSTDIR\System.IO.Pipelines.xml"
  Delete "$INSTDIR\Microsoft.Bcl.AsyncInterfaces.dll"
  Delete "$INSTDIR\Microsoft.Bcl.AsyncInterfaces.xml"
  Delete "$INSTDIR\System.Text.Json.dll"
  Delete "$INSTDIR\System.Text.Json.xml" 
  Delete "$INSTDIR\System.Memory.dll" 
  Delete "$INSTDIR\System.Memory.xml"
  Delete "$INSTDIR\System.Runtime.CompilerServices.Unsafe.dll"
  Delete "$INSTDIR\System.Runtime.CompilerServices.Unsafe.xml"
  Delete "$INSTDIR\System.Buffers.dll" 
  Delete "$INSTDIR\System.Buffers.xml" 
  Delete "$INSTDIR\System.Threading.Tasks.Extensions.dll"
  Delete "$INSTDIR\System.Threading.Tasks.Extensions.xml"
  Delete "$INSTDIR\System.ValueTuple.dll"
  Delete "$INSTDIR\System.ValueTuple.xml"
  Delete "$INSTDIR\System.Numerics.Vectors.dll"
  Delete "$INSTDIR\System.Numerics.Vectors.xml"

  Delete "$INSTDIR\Uninstall.exe"

  # Удаление ярлыков
  Delete "$DESKTOP\TempViewer.lnk"
  Delete "$SMPROGRAMS\${APPNAME}\TempViewer.lnk"
  Delete "$SMPROGRAMS\${APPNAME}\Uninstall.lnk"

  # Удаление папки меню Пуск
  RMDir "$SMPROGRAMS\${APPNAME}"

  # Удаление папки установки
  RMDir "$INSTDIR"
  
   # Удаление папки в %LOCALAPPDATA%
  RMDir /r "$LOCALAPPDATA\TempViewer"

  # Удаление записей из реестра
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"

SectionEnd
