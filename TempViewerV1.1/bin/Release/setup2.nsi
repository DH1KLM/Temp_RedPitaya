# Имя выходного файла установщика
Outfile "TempViewerRPSetup.exe"

# Путь установки (по умолчанию Program Files)
InstallDir "$PROGRAMFILES\TempViewer"

# Уровень прав администратора
RequestExecutionLevel admin

# Имя приложения, издателя и версия
!define APPNAME "TempViewer"
!define COMPANYNAME "LIKSHADOW" ; замени на свою компанию, если нужно
!define APPVERSION "3.0.0.0"

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
